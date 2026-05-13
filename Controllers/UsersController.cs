using System.Security.Claims;
using System.Text.RegularExpressions;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    ApplicationDbContext dbContext) : ControllerBase
{
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
}
