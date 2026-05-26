using System.Security.Claims;
using System.Text.RegularExpressions;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string FakeCharacterDataSource = "dev-fake";
    private const string RegistrationStatusComplete = "complete";
    private static readonly string[] JobCodes =
    [
        "WAR", "MNK", "WHM", "BLM", "RDM", "THF", "PLD", "DRK", "BST", "BRD", "RNG", "SAM", "NIN", "DRG", "SMN"
    ];

    [HttpPost("dev/fake-players")]
    public async Task<IActionResult> GenerateFakePlayers([FromBody] GenerateFakePlayersRequest? request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        User? currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!currentUser.IsActive)
        {
            return Forbid();
        }

        if (!CanManageRoles(currentUser))
        {
            return Forbid();
        }

        var normalizedRequest = NormalizeFakePlayersRequest(request);
        if (normalizedRequest.MinCharactersPerUser > normalizedRequest.MaxCharactersPerUser)
        {
            return BadRequest(new { message = "Minimum characters per user must be less than or equal to maximum characters per user." });
        }

        var createdUsers = new List<FakeUserSummaryResponse>();
        var roleBreakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < normalizedRequest.Count; index++)
        {
            var role = PickRole(index, normalizedRequest.EnsureAllRolesRepresented);
            var displayName = GenerateDisplayName();
            var discordId = GenerateFakeDiscordId();
            var preferredJobs = PickPreferredJobs();

            var fakeUser = new User
            {
                DiscordId = discordId,
                DisplayName = displayName,
                Role = role,
                IsActive = true,
                IsRegistrationComplete = true,
                DkpBalance = Random.Shared.Next(0, 501),
                CreatedAt = DateTime.UtcNow,
                RegistrationCompletedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(fakeUser);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var jobCode in preferredJobs)
            {
                dbContext.UserPreferredJobs.Add(new UserPreferredJob
                {
                    UserId = fakeUser.Id,
                    JobCode = jobCode
                });
            }

            var characterCount = Random.Shared.Next(normalizedRequest.MinCharactersPerUser, normalizedRequest.MaxCharactersPerUser + 1);
            var mainCharacterIndex = Random.Shared.Next(0, characterCount);

            for (var characterIndex = 0; characterIndex < characterCount; characterIndex++)
            {
                dbContext.Characters.Add(BuildFakeCharacter(fakeUser.Id, displayName, characterIndex, characterIndex == mainCharacterIndex));
            }

            createdUsers.Add(new FakeUserSummaryResponse(fakeUser.Id, fakeUser.DisplayName, fakeUser.DiscordId, fakeUser.Role, characterCount));

            roleBreakdown[role] = roleBreakdown.GetValueOrDefault(role) + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new GenerateFakePlayersResponse(
            createdUsers.Count,
            createdUsers.Sum(user => user.CharacterCount),
            createdUsers,
            roleBreakdown));
    }

    [HttpPost("impersonation/start")]
    public async Task<IActionResult> StartImpersonation([FromBody] StartImpersonationRequest? request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (request is null || request.UserId <= 0)
        {
            return BadRequest(new { message = "A valid target user id is required." });
        }

        if (GetImpersonatorUserId(User).HasValue)
        {
            return BadRequest(new { message = "Already impersonating a user. Stop impersonation before switching again." });
        }

        User? currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!currentUser.IsActive)
        {
            return Forbid();
        }

        if (!CanManageRoles(currentUser))
        {
            return Forbid();
        }

        if (currentUser.Id == request.UserId)
        {
            return BadRequest(new { message = "You are already signed in as that user." });
        }

        User? targetUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == request.UserId, cancellationToken);

        if (targetUser is null)
        {
            return NotFound(new { message = "Target user was not found." });
        }

        if (!targetUser.IsActive)
        {
            return BadRequest(new { message = "Target user is inactive." });
        }

        if (!targetUser.IsRegistrationComplete)
        {
            return BadRequest(new { message = "Target user must have completed registration before impersonation." });
        }

        await SignInAsUserAsync(targetUser, currentUser);

        return Ok(new StartImpersonationResponse(targetUser.Id, targetUser.DisplayName, targetUser.Role));
    }

    [HttpPost("impersonation/stop")]
    public async Task<IActionResult> StopImpersonation(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        int? impersonatorUserId = GetImpersonatorUserId(User);
        if (!impersonatorUserId.HasValue)
        {
            return BadRequest(new { message = "No active impersonation session was found." });
        }

        User? impersonator = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == impersonatorUserId.Value, cancellationToken);

        if (impersonator is null)
        {
            return Unauthorized(new { message = "Original user could not be resolved." });
        }

        if (!impersonator.IsActive)
        {
            return Forbid();
        }

        await SignInAsUserAsync(impersonator, null);

        return Ok(new StopImpersonationResponse(impersonator.Id, impersonator.DisplayName, impersonator.Role));
    }

    [HttpGet("access")]
    public async Task<IActionResult> GetAccessOverview(CancellationToken cancellationToken)
    {
        User? currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!currentUser.IsActive)
        {
            return Forbid();
        }

        if (!CanManageRoles(currentUser))
        {
            return Forbid();
        }

        List<AccessUserSummaryResponse> users = await dbContext.Users
            .OrderBy(user => user.DisplayName)
            .Select(user => new AccessUserSummaryResponse(
                user.Id,
                user.DisplayName,
                user.DiscordId,
                user.Role,
                user.IsActive,
                user.IsRegistrationComplete))
            .ToListAsync(cancellationToken);

        IReadOnlyList<AccessRoleDefinitionResponse> roles = AppRoles.All
            .Select(role => new AccessRoleDefinitionResponse(
                role,
                RolePermissions.GetPermissionsForRole(role)))
            .ToList();

        return Ok(new AccessOverviewResponse(
            AppPermissions.All,
            roles,
            users));
    }

    [HttpPut("{userId:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Role payload is required." });
        }

        string requestedRole = (request.Role ?? string.Empty).Trim();
        if (!RolePermissions.IsValidRole(requestedRole))
        {
            return BadRequest(new { message = "Role is invalid." });
        }

        User? currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!currentUser.IsActive)
        {
            return Forbid();
        }

        if (!CanManageRoles(currentUser))
        {
            return Forbid();
        }

        User? targetUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (targetUser is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        targetUser.Role = requestedRole;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UpdateUserRoleResponse(
            targetUser.Id,
            targetUser.DisplayName,
            targetUser.Role,
            RolePermissions.GetPermissionsForRole(targetUser.Role)));
    }

    [HttpGet("roster")]
    public async Task<IActionResult> GetRoster(CancellationToken cancellationToken)
    {
        User? currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!currentUser.IsActive)
        {
            return Forbid();
        }

        List<User> users = await dbContext.Users
            .Where(u => u.IsActive && u.IsRegistrationComplete)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

        List<int> userIds = users.Select(u => u.Id).ToList();

        List<Character> characters = await dbContext.Characters
            .Where(c => userIds.Contains(c.OwnerUserId) && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        List<UserPreferredJob> preferredJobs = await dbContext.UserPreferredJobs
            .Where(j => userIds.Contains(j.UserId))
            .ToListAsync(cancellationToken);

        Dictionary<int, List<Character>> charactersByUser = characters
            .GroupBy(c => c.OwnerUserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Dictionary<int, List<string>> preferredJobsByUser = preferredJobs
            .GroupBy(j => j.UserId)
            .ToDictionary(g => g.Key, g => g.Select(j => j.JobCode).ToList());

        List<RosterMemberResponse> members = users
            .Select(u =>
            {
                List<Character> userCharacters = charactersByUser.GetValueOrDefault(u.Id, []);
                Character? mainCharacter = userCharacters.FirstOrDefault(x => x.IsMain == true);

                if (mainCharacter is null)
                {
                    return null;
                }

                List<string> level75Jobs = userCharacters
                    .SelectMany(GetLevel75Jobs)
                    .Distinct()
                    .Order()
                    .ToList();

                ContentAccessResponse access = DeriveContentAccess(userCharacters);
                List<string> jobs = preferredJobsByUser.GetValueOrDefault(u.Id, []);

                return new RosterMemberResponse(
                    u.Id,
                    mainCharacter.Id,
                    mainCharacter.Name,
                    u.DisplayName,
                    jobs,
                    level75Jobs,
                    u.DkpBalance,
                    access);
            })
            .Where(m => m is not null)
            .ToList()!;

        return Ok(new RosterResponse(members));
    }

    private static IEnumerable<string> GetLevel75Jobs(Character character)
    {
        if (character.JobWarLevel >= 75) yield return "WAR";
        if (character.JobMnkLevel >= 75) yield return "MNK";
        if (character.JobWhmLevel >= 75) yield return "WHM";
        if (character.JobBlmLevel >= 75) yield return "BLM";
        if (character.JobRdmLevel >= 75) yield return "RDM";
        if (character.JobThfLevel >= 75) yield return "THF";
        if (character.JobPldLevel >= 75) yield return "PLD";
        if (character.JobDrkLevel >= 75) yield return "DRK";
        if (character.JobBstLevel >= 75) yield return "BST";
        if (character.JobBrdLevel >= 75) yield return "BRD";
        if (character.JobRngLevel >= 75) yield return "RNG";
        if (character.JobSamLevel >= 75) yield return "SAM";
        if (character.JobNinLevel >= 75) yield return "NIN";
        if (character.JobDrgLevel >= 75) yield return "DRG";
        if (character.JobSmnLevel >= 75) yield return "SMN";
    }

    private static ContentAccessResponse DeriveContentAccess(IReadOnlyList<Character> characters)
    {
        bool sky = characters.Any(c => IsAtLeastMission(c.RiseOfTheZilartMission, 13));
        bool sea = characters.Any(c => IsAtLeastMission(c.ChainsOfPromathiaMission, 7, 5));
        bool limbus = sea;

        bool dynamisCities = characters.Any(c =>
            IsAtLeastMission(c.SandOriaMission, 5, 2) ||
            IsAtLeastMission(c.BastokMission, 5, 2) ||
            IsAtLeastMission(c.WindurstMission, 5, 2));

        bool dynamisIcelands = characters.Any(c =>
            c.DynamisSandOria && c.DynamisBastok && c.DynamisWindurst && c.DynamisJeuno);

        bool dynamisDreamlands = characters.Any(c => IsAtLeastMission(c.ChainsOfPromathiaMission, 3, 5));

        bool dynamisTavnazia = characters.Any(c =>
            c.DynamisValkurm && c.DynamisBuburimu && c.DynamisQufim);

        return new ContentAccessResponse(sky, sea, limbus, dynamisCities, dynamisIcelands, dynamisDreamlands, dynamisTavnazia);
    }

    private static bool IsAtLeastMission(string? value, int requiredMajor, int requiredMinor = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().Replace('-', '.');
        Match match = Regex.Match(normalized, @"(\d+)(?:\.(\d+))?");
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups[1].Value, out int major))
        {
            return false;
        }

        int.TryParse(match.Groups[2].Value, out int minor);

        return major > requiredMajor || (major == requiredMajor && minor >= requiredMinor);
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        string? discordId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return null;
        }

        return await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId, cancellationToken);
    }

    private static bool CanManageRoles(User user)
    {
        return user.IsActive && RolePermissions.RoleHasPermission(user.Role, AppPermissions.ManageRoles);
    }

    private async Task SignInAsUserAsync(User targetUser, User? impersonator)
    {
        var claims = BuildClaimsForUser(targetUser, impersonator);
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var accessToken = authenticateResult.Properties?.GetTokenValue("access_token");
        var tokenType = authenticateResult.Properties?.GetTokenValue("token_type") ?? "Bearer";

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = accessToken },
                new AuthenticationToken { Name = "token_type", Value = tokenType }
            });
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private static IReadOnlyList<Claim> BuildClaimsForUser(User targetUser, User? impersonator)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, targetUser.DiscordId),
            new(ClaimTypes.Name, targetUser.DisplayName),
            new(ClaimTypes.Role, targetUser.Role),
            new(AppClaimTypes.UserId, targetUser.Id.ToString()),
            new(AppClaimTypes.RegistrationStatus, RegistrationStatusComplete)
        };

        foreach (var permission in RolePermissions.GetPermissionsForRole(targetUser.Role))
        {
            claims.Add(new Claim(AppClaimTypes.Permission, permission));
        }

        if (impersonator is not null)
        {
            claims.Add(new Claim(AppClaimTypes.ImpersonatorUserId, impersonator.Id.ToString()));
            claims.Add(new Claim(AppClaimTypes.ImpersonatorDiscordId, impersonator.DiscordId));
            claims.Add(new Claim(AppClaimTypes.ImpersonatorDisplayName, impersonator.DisplayName));
        }

        return claims;
    }

    private static int? GetImpersonatorUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AppClaimTypes.ImpersonatorUserId);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static GenerateFakePlayersRequest NormalizeFakePlayersRequest(GenerateFakePlayersRequest? request)
    {
        if (request is null)
        {
            return new GenerateFakePlayersRequest();
        }

        return new GenerateFakePlayersRequest
        {
            Count = Math.Clamp(request.Count, 1, 100),
            MinCharactersPerUser = Math.Clamp(request.MinCharactersPerUser, 1, 3),
            MaxCharactersPerUser = Math.Clamp(request.MaxCharactersPerUser, 1, 3),
            EnsureAllRolesRepresented = request.EnsureAllRolesRepresented
        };
    }

    private static string GenerateFakeDiscordId()
    {
        return $"fake-{Guid.NewGuid():N}";
    }

    private static string GenerateDisplayName()
    {
        var adjective = FakeNameAdjectives[Random.Shared.Next(FakeNameAdjectives.Length)];
        var noun = FakeNameNouns[Random.Shared.Next(FakeNameNouns.Length)];
        var suffix = Random.Shared.Next(100, 999);
        return $"{adjective} {noun} {suffix}";
    }

    private static Character BuildFakeCharacter(int ownerUserId, string ownerDisplayName, int sequence, bool isMain)
    {
        var jobLevels = GenerateJobLevels();

        return new Character
        {
            Name = GenerateCharacterName(ownerDisplayName, sequence),
            OwnerUserId = ownerUserId,
            IsActive = true,
            IsMain = isMain,
            DataSource = FakeCharacterDataSource,
            LastSyncedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SandOriaMission = PickMission(["5-1", "5-2", "6-1", "7-1"]),
            BastokMission = PickMission(["5-1", "5-2", "6-1", "7-1"]),
            WindurstMission = PickMission(["5-1", "5-2", "6-1", "7-1"]),
            RiseOfTheZilartMission = PickMission(["10", "12", "13", "14"]),
            ChainsOfPromathiaMission = PickMission(["2-5", "3-5", "5-3", "7-5"]),
            EpilogueMission = PickMission(["Prologue", "The Last Verse", "A New Horizon"]),
            DynamisSandOria = Random.Shared.NextDouble() >= 0.3,
            DynamisBastok = Random.Shared.NextDouble() >= 0.35,
            DynamisWindurst = Random.Shared.NextDouble() >= 0.35,
            DynamisJeuno = Random.Shared.NextDouble() >= 0.4,
            DynamisBeaucedine = Random.Shared.NextDouble() >= 0.45,
            DynamisXarcabard = Random.Shared.NextDouble() >= 0.5,
            DynamisValkurm = Random.Shared.NextDouble() >= 0.45,
            DynamisBuburimu = Random.Shared.NextDouble() >= 0.45,
            DynamisQufim = Random.Shared.NextDouble() >= 0.45,
            DynamisTavnazia = Random.Shared.NextDouble() >= 0.6,
            JobWarLevel = jobLevels["WAR"],
            JobMnkLevel = jobLevels["MNK"],
            JobWhmLevel = jobLevels["WHM"],
            JobBlmLevel = jobLevels["BLM"],
            JobRdmLevel = jobLevels["RDM"],
            JobThfLevel = jobLevels["THF"],
            JobPldLevel = jobLevels["PLD"],
            JobDrkLevel = jobLevels["DRK"],
            JobBstLevel = jobLevels["BST"],
            JobBrdLevel = jobLevels["BRD"],
            JobRngLevel = jobLevels["RNG"],
            JobSamLevel = jobLevels["SAM"],
            JobNinLevel = jobLevels["NIN"],
            JobDrgLevel = jobLevels["DRG"],
            JobSmnLevel = jobLevels["SMN"],
            CraftSmithingLevel = Random.Shared.Next(0, 101),
            CraftGoldsmithingLevel = Random.Shared.Next(0, 101),
            CraftClothcraftLevel = Random.Shared.Next(0, 101),
            CraftLeathercraftLevel = Random.Shared.Next(0, 101),
            CraftBonecraftLevel = Random.Shared.Next(0, 101),
            CraftWoodworkingLevel = Random.Shared.Next(0, 101),
            CraftAlchemyLevel = Random.Shared.Next(0, 101),
            CraftCookingLevel = Random.Shared.Next(0, 101),
            CraftFishingLevel = Random.Shared.Next(0, 101)
        };
    }

    private static Dictionary<string, int> GenerateJobLevels()
    {
        var levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in JobCodes)
        {
            levels[job] = Random.Shared.Next(1, 76);
        }

        return levels;
    }

    private static string GenerateCharacterName(string ownerDisplayName, int sequence)
    {
        var compactOwnerName = new string(ownerDisplayName.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(compactOwnerName))
        {
            compactOwnerName = "Player";
        }

        var suffix = Random.Shared.Next(10, 99);
        return $"{compactOwnerName[..Math.Min(compactOwnerName.Length, 8)]}{suffix}{sequence + 1}";
    }

    private static string PickMission(IReadOnlyList<string> options)
    {
        return options[Random.Shared.Next(options.Count)];
    }

    private static List<string> PickPreferredJobs()
    {
        var preferredJobCount = Random.Shared.Next(1, 4);
        return JobCodes
            .OrderBy(_ => Random.Shared.Next())
            .Take(preferredJobCount)
            .OrderBy(jobCode => jobCode)
            .ToList();
    }

    private static string PickRole(int index, bool ensureAllRolesRepresented)
    {
        if (ensureAllRolesRepresented && index < AppRoles.All.Count)
        {
            return AppRoles.All[index];
        }

        return AppRoles.All[Random.Shared.Next(AppRoles.All.Count)];
    }

    private static readonly string[] FakeNameAdjectives =
    [
        "Azure", "Scarlet", "Silent", "Arcane", "Radiant", "Crimson", "Swift", "Iron", "Golden", "Storm"
    ];

    private static readonly string[] FakeNameNouns =
    [
        "Warden", "Sentinel", "Vanguard", "Drifter", "Scholar", "Ranger", "Templar", "Mystic", "Reaver", "Nomad"
    ];

    public sealed record RosterResponse(IReadOnlyList<RosterMemberResponse> Members);

    public sealed record RosterMemberResponse(
        int MemberId,
        int CharacterId,
        string CharacterName,
        string DiscordAlias,
        IReadOnlyList<string> PreferredJobs,
        IReadOnlyList<string> Level75Jobs,
        int? DkpTotal,
        ContentAccessResponse ContentAccess);

    public sealed record ContentAccessResponse(
        bool Sky,
        bool Sea,
        bool Limbus,
        bool DynamisCities,
        bool DynamisIcelands,
        bool DynamisDreamlands,
        bool DynamisTavnazia);

    public sealed record AccessOverviewResponse(
        IReadOnlyList<string> Permissions,
        IReadOnlyList<AccessRoleDefinitionResponse> Roles,
        IReadOnlyList<AccessUserSummaryResponse> Users);

    public sealed record AccessRoleDefinitionResponse(
        string Role,
        IReadOnlyList<string> Permissions);

    public sealed record AccessUserSummaryResponse(
        int Id,
        string DisplayName,
        string DiscordId,
        string Role,
        bool IsActive,
        bool IsRegistrationComplete);

    public sealed class GenerateFakePlayersRequest
    {
        public int Count { get; set; } = 12;

        public int MinCharactersPerUser { get; set; } = 1;

        public int MaxCharactersPerUser { get; set; } = 3;

        public bool EnsureAllRolesRepresented { get; set; } = true;
    }

    public sealed record GenerateFakePlayersResponse(
        int UsersCreated,
        int CharactersCreated,
        IReadOnlyList<FakeUserSummaryResponse> Users,
        IReadOnlyDictionary<string, int> RoleBreakdown);

    public sealed record FakeUserSummaryResponse(
        int Id,
        string DisplayName,
        string DiscordId,
        string Role,
        int CharacterCount);

    public sealed class StartImpersonationRequest
    {
        public int UserId { get; set; }
    }

    public sealed record StartImpersonationResponse(
        int UserId,
        string DisplayName,
        string Role);

    public sealed record StopImpersonationResponse(
        int UserId,
        string DisplayName,
        string Role);

    public sealed class UpdateUserRoleRequest
    {
        public string? Role { get; set; }
    }

    public sealed record UpdateUserRoleResponse(
        int Id,
        string DisplayName,
        string Role,
        IReadOnlyList<string> Permissions);
}
