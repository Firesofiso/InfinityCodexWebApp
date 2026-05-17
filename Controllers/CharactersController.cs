using System.Security.Claims;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using InfinityCodexWebApp.Integrations.Horizon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Authorize]
[Route("api/characters")]
public sealed class CharactersController(
    ApplicationDbContext dbContext,
    IHorizonCharacterLookupClient horizonCharacterLookupClient,
    IOptions<HorizonApiOptions> options,
    ILogger<CharactersController> logger) : ControllerBase
{
    private const string WishlistStateNeeded = "needed";
    private const int MissionTextMaxLength = 128;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var query = (search ?? string.Empty).Trim();

        if (query.Length < config.MinimumSearchLength)
        {
            return BadRequest(new
            {
                message = $"Search must be at least {config.MinimumSearchLength} characters.",
                minimumSearchLength = config.MinimumSearchLength
            });
        }

        if (query.Length > config.MaximumSearchLength)
        {
            return BadRequest(new
            {
                message = $"Search cannot exceed {config.MaximumSearchLength} characters.",
                maximumSearchLength = config.MaximumSearchLength
            });
        }

        try
        {
            var results = await horizonCharacterLookupClient.SearchCharactersAsync(query, cancellationToken);

            return Ok(new
            {
                query,
                count = results.Count,
                results = results.Select(result => new
                {
                    characterId = result.CharacterId,
                    name = result.Name,
                    currentJob = result.CurrentJob,
                    portraitUrl = result.PortraitUrl,
                    jobString = result.RawJobString
                })
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Unable to search Horizon characters for query {Query}", query);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Character search is temporarily unavailable. Please try again."
            });
        }
    }

    [HttpGet("workspace")]
    public async Task<IActionResult> GetWorkspaceCharacters(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        List<Character> characterEntities = await dbContext.Characters
            .Where(character => character.OwnerUserId == user.Id && character.IsActive)
            .OrderBy(character => character.Name)
            .ToListAsync(cancellationToken);

        List<CharacterWorkspaceListItemResponse> characters = characterEntities
            .Select(character => new CharacterWorkspaceListItemResponse(
                character.Id,
                character.Name,
                character.IsActive,
                character.PortraitUrl,
                character.LastSyncedAt,
                user.DkpBalance))
            .ToList();

        int? mainCharacterId = characterEntities.FirstOrDefault(c => c.IsMain)?.Id;

        return Ok(new CharacterWorkspaceListResponse(characters, mainCharacterId));
    }

    [HttpPut("workspace/{characterId:int}/set-main")]
    public async Task<IActionResult> SetMainCharacter(int characterId, CancellationToken cancellationToken)
    {
        User? user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        Character? character = await dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        await dbContext.Characters
            .Where(c => c.OwnerUserId == character.OwnerUserId && c.IsMain)
            .ExecuteUpdateAsync(setter => setter.SetProperty(c => c.IsMain, false), cancellationToken);

        character.IsMain = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("workspace/{characterId:int}")]
    public async Task<IActionResult> GetWorkspaceCharacter(int characterId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        var character = await dbContext.Characters
            .FirstOrDefaultAsync(entity => entity.Id == characterId && entity.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        var ownedCharacterIds = await dbContext.Characters
            .Where(entity => entity.OwnerUserId == character.OwnerUserId && entity.IsActive)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        var selectedItemIdsTask = dbContext.CharacterItemNeeds
            .Where(entity => entity.CharacterId == character.Id && entity.State == WishlistStateNeeded)
            .OrderBy(entity => entity.ItemId)
            .Select(entity => entity.ItemId)
            .ToListAsync(cancellationToken);

        var wishlistAssignmentsTask = dbContext.CharacterItemNeeds
            .Where(entity => ownedCharacterIds.Contains(entity.CharacterId) && entity.State == WishlistStateNeeded)
            .Select(entity => new { entity.ItemId, entity.CharacterId })
            .ToListAsync(cancellationToken);

        var activeItemsTask = dbContext.Items
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var allowedJobsTask = dbContext.ItemAllowedJobs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var itemSourcesTask = (from itemSource in dbContext.ItemSources.AsNoTracking()
                               join contentSource in dbContext.ContentSources.AsNoTracking()
                                   on itemSource.ContentSourceId equals contentSource.Id
                               join contentGroup in dbContext.ContentGroups.AsNoTracking()
                                   on contentSource.ContentGroupId equals contentGroup.Id
                               select new ItemSourceReferenceRow(
                                   itemSource.ItemId,
                                   contentSource.Id,
                                   contentSource.Tag,
                                   contentSource.Name,
                                   contentGroup.Id,
                                   contentGroup.Tag,
                                   contentGroup.Name))
            .ToListAsync(cancellationToken);

        await Task.WhenAll(selectedItemIdsTask, wishlistAssignmentsTask, activeItemsTask, allowedJobsTask, itemSourcesTask);

        HorizonCharacterDetailResponse? horizonResponse = null;
        string? horizonError = null;

        try
        {
            var horizonCharacter = await horizonCharacterLookupClient.GetCharacterAsync(character.Name, cancellationToken);
            if (horizonCharacter is null)
            {
                horizonError = "Character details could not be found on HorizonXI.";
            }
            else
            {
                horizonResponse = MapHorizonCharacter(horizonCharacter);
                character.PortraitUrl = horizonCharacter.PortraitUrl;
                character.LastSyncedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Unable to load Horizon detail for character {CharacterName}", character.Name);
            horizonError = "Live HorizonXI data is temporarily unavailable.";
        }

        var allowedJobsByItem = allowedJobsTask.Result
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(row => row.JobCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(jobCode => jobCode)
                    .ToList());

        var sourceReferencesByItem = itemSourcesTask.Result
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CharacterWishlistSourceResponse>)group
                    .GroupBy(row => row.SourceId)
                    .Select(rows => rows.First())
                    .OrderBy(row => row.GroupTag)
                    .ThenBy(row => string.IsNullOrWhiteSpace(row.SourceTag) ? row.SourceName : row.SourceTag)
                    .Select(row => new CharacterWishlistSourceResponse(
                        row.SourceId,
                        row.SourceName,
                        row.SourceTag,
                        new CharacterWishlistContentGroupResponse(row.GroupId, row.GroupName, row.GroupTag)))
                    .ToList());

        var wishlistItems = activeItemsTask.Result
            .Select(item => new CharacterWishlistItemResponse(
                item.Id,
                item.Name,
                item.RequiredLevel,
                item.Slot,
                item.Notes,
                allowedJobsByItem.GetValueOrDefault(item.Id, Array.Empty<string>()),
                sourceReferencesByItem.GetValueOrDefault(item.Id, Array.Empty<CharacterWishlistSourceResponse>())))
            .ToList();

        var assignments = wishlistAssignmentsTask.Result
            .GroupBy(row => row.ItemId)
            .Select(group => new CharacterWishlistAssignmentResponse(
                group.Key,
                group.Select(row => row.CharacterId)
                    .Distinct()
                    .OrderBy(characterIdValue => characterIdValue)
                    .ToList()))
            .OrderBy(row => row.ItemId)
            .ToList();

        return Ok(new CharacterWorkspaceDetailResponse(
            new CharacterWorkspaceListItemResponse(character.Id, character.Name, character.IsActive, character.PortraitUrl, character.LastSyncedAt, user.DkpBalance),
            horizonResponse,
            horizonError,
            new CharacterMissionProgressResponse(
                character.SandOriaMission,
                character.BastokMission,
                character.WindurstMission,
                character.RiseOfTheZilartMission,
                character.ChainsOfPromathiaMission,
                character.EpilogueMission,
                character.UpdatedAt),
            new CharacterDynamisClearsResponse(
                character.DynamisSandOria,
                character.DynamisBastok,
                character.DynamisWindurst,
                character.DynamisJeuno,
                character.DynamisBeaucedine,
                character.DynamisXarcabard,
                character.DynamisValkurm,
                character.DynamisBuburimu,
                character.DynamisQufim,
                character.DynamisTavnazia),
            new CharacterWishlistResponse(selectedItemIdsTask.Result, wishlistItems, assignments)));
    }

    [HttpPut("workspace/{characterId:int}/missions")]
    public async Task<IActionResult> UpdateMissionProgress(int characterId, [FromBody] UpdateCharacterMissionProgressRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Mission payload is required." });
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        var character = await dbContext.Characters
            .FirstOrDefaultAsync(entity => entity.Id == characterId && entity.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        var sandOriaMission = NormalizeMissionValue(request.SandOriaMission);
        var bastokMission = NormalizeMissionValue(request.BastokMission);
        var windurstMission = NormalizeMissionValue(request.WindurstMission);
        var riseOfTheZilartMission = NormalizeMissionValue(request.RiseOfTheZilartMission);
        var chainsOfPromathiaMission = NormalizeMissionValue(request.ChainsOfPromathiaMission);
        var epilogueMission = NormalizeMissionValue(request.EpilogueMission);

        if (!ValidateMissionLength(sandOriaMission)
            || !ValidateMissionLength(bastokMission)
            || !ValidateMissionLength(windurstMission)
            || !ValidateMissionLength(riseOfTheZilartMission)
            || !ValidateMissionLength(chainsOfPromathiaMission)
            || !ValidateMissionLength(epilogueMission))
        {
            return BadRequest(new { message = $"Mission values must be {MissionTextMaxLength} characters or fewer." });
        }

        character.SandOriaMission = sandOriaMission;
        character.BastokMission = bastokMission;
        character.WindurstMission = windurstMission;
        character.RiseOfTheZilartMission = riseOfTheZilartMission;
        character.ChainsOfPromathiaMission = chainsOfPromathiaMission;
        character.EpilogueMission = epilogueMission;
        character.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CharacterMissionProgressResponse(
            character.SandOriaMission,
            character.BastokMission,
            character.WindurstMission,
            character.RiseOfTheZilartMission,
            character.ChainsOfPromathiaMission,
            character.EpilogueMission,
            character.UpdatedAt));
    }

    [HttpPut("workspace/{characterId:int}/dynamis")]
    public async Task<IActionResult> UpdateDynamisClears(int characterId, [FromBody] UpdateDynamisClearsRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Dynamis clears payload is required." });
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        var character = await dbContext.Characters
            .FirstOrDefaultAsync(entity => entity.Id == characterId && entity.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        character.DynamisSandOria = request.DynamisSandOria;
        character.DynamisBastok = request.DynamisBastok;
        character.DynamisWindurst = request.DynamisWindurst;
        character.DynamisJeuno = request.DynamisJeuno;
        character.DynamisBeaucedine = request.DynamisBeaucedine;
        character.DynamisXarcabard = request.DynamisXarcabard;
        character.DynamisValkurm = request.DynamisValkurm;
        character.DynamisBuburimu = request.DynamisBuburimu;
        character.DynamisQufim = request.DynamisQufim;
        character.DynamisTavnazia = request.DynamisTavnazia;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CharacterDynamisClearsResponse(
            character.DynamisSandOria,
            character.DynamisBastok,
            character.DynamisWindurst,
            character.DynamisJeuno,
            character.DynamisBeaucedine,
            character.DynamisXarcabard,
            character.DynamisValkurm,
            character.DynamisBuburimu,
            character.DynamisQufim,
            character.DynamisTavnazia));
    }

    [HttpPut("workspace/{characterId:int}/wishlist")]
    public async Task<IActionResult> UpdateWishlist(int characterId, [FromBody] UpdateCharacterWishlistRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Wishlist payload is required." });
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        var character = await dbContext.Characters
            .FirstOrDefaultAsync(entity => entity.Id == characterId && entity.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        var ownedCharacterIds = await dbContext.Characters
            .Where(entity => entity.OwnerUserId == character.OwnerUserId && entity.IsActive)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        var normalizedAssignments = request.Assignments
            .Where(assignment => assignment is not null)
            .Select(assignment => new CharacterWishlistAssignmentUpdate(
                assignment.ItemId,
                assignment.CharacterIds
                    .Distinct()
                    .OrderBy(characterIdValue => characterIdValue)
                    .ToList()))
            .Where(assignment => assignment.CharacterIds.Count > 0)
            .GroupBy(assignment => assignment.ItemId)
            .Select(group => new CharacterWishlistAssignmentUpdate(
                group.Key,
                group.SelectMany(row => row.CharacterIds)
                    .Distinct()
                    .OrderBy(characterIdValue => characterIdValue)
                    .ToList()))
            .OrderBy(assignment => assignment.ItemId)
            .ToList();

        if (normalizedAssignments.Count == 0 && request.ItemIds.Count > 0)
        {
            normalizedAssignments = request.ItemIds
                .Distinct()
                .OrderBy(itemId => itemId)
                .Select(itemId => new CharacterWishlistAssignmentUpdate(itemId, new List<int> { character.Id }))
                .ToList();
        }

        var normalizedItemIds = normalizedAssignments
            .Select(assignment => assignment.ItemId)
            .Distinct()
            .OrderBy(itemId => itemId)
            .ToList();

        if (normalizedAssignments.Any(assignment => assignment.CharacterIds.Any(characterIdValue => !ownedCharacterIds.Contains(characterIdValue))))
        {
            return BadRequest(new { message = "Wishlist assignments contain one or more invalid characters." });
        }

        var validItemIds = await dbContext.Items
            .Where(item => item.IsActive && normalizedItemIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (validItemIds.Count != normalizedItemIds.Count)
        {
            return BadRequest(new { message = "Wishlist contains one or more invalid items." });
        }

        var existingNeeds = await dbContext.CharacterItemNeeds
            .Where(entity => ownedCharacterIds.Contains(entity.CharacterId) && entity.State == WishlistStateNeeded)
            .ToListAsync(cancellationToken);

        if (existingNeeds.Count > 0)
        {
            dbContext.CharacterItemNeeds.RemoveRange(existingNeeds);
        }

        foreach (var assignment in normalizedAssignments)
        {
            foreach (var assignedCharacterId in assignment.CharacterIds)
            {
                dbContext.CharacterItemNeeds.Add(new CharacterItemNeed
                {
                    CharacterId = assignedCharacterId,
                    ItemId = assignment.ItemId,
                    State = WishlistStateNeeded
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var selectedItemIds = normalizedAssignments
            .Where(assignment => assignment.CharacterIds.Contains(character.Id))
            .Select(assignment => assignment.ItemId)
            .OrderBy(itemId => itemId)
            .ToList();

        var responseAssignments = normalizedAssignments
            .Select(assignment => new CharacterWishlistAssignmentResponse(assignment.ItemId, assignment.CharacterIds))
            .ToList();

        return Ok(new CharacterWishlistSelectionResponse(selectedItemIds, responseAssignments));
    }

    [HttpPost("workspace/{characterId:int}/wishlist/{itemId:int}/obtained")]
    public async Task<IActionResult> MarkWishlistItemObtained(int characterId, int itemId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        var character = await dbContext.Characters
            .FirstOrDefaultAsync(entity => entity.Id == characterId && entity.IsActive, cancellationToken);

        if (character is null || !CanAccessCharacter(user, character))
        {
            return NotFound(new { message = "Character was not found." });
        }

        var need = await dbContext.CharacterItemNeeds
            .FirstOrDefaultAsync(entity => entity.CharacterId == characterId && entity.ItemId == itemId && entity.State == WishlistStateNeeded, cancellationToken);

        if (need is null)
        {
            return NotFound(new { message = "Gear need was not found." });
        }

        need.State = "obtained";
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var discordId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return null;
        }

        return await dbContext.Users.FirstOrDefaultAsync(entity => entity.DiscordId == discordId, cancellationToken);
    }

    private static bool CanAccessCharacter(User user, Character character)
    {
        if (character.OwnerUserId == user.Id)
        {
            return true;
        }

        return user.IsActive
            && RolePermissions.RoleHasPermission(user.Role, AppPermissions.ManageUsers);
    }

    private static string? NormalizeMissionValue(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool ValidateMissionLength(string? value)
    {
        return value is null || value.Length <= MissionTextMaxLength;
    }

    private static HorizonCharacterDetailResponse MapHorizonCharacter(HorizonCharacterDetailResult character)
    {
        var jobs = character.Jobs
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .Select(entry => new HorizonJobLevelResponse(entry.Key, entry.Value))
            .ToList();

        return new HorizonCharacterDetailResponse(
            character.Name,
            character.Nation,
            character.Rank,
            character.Mentor,
            character.Settings,
            character.JobString,
            character.Avatar,
            character.PortraitUrl,
            character.SeacomType,
            character.SeacomMessage,
            character.Online,
            jobs);
    }

    private sealed record ItemSourceReferenceRow(
        int ItemId,
        int SourceId,
        string SourceTag,
        string SourceName,
        int GroupId,
        string GroupTag,
        string GroupName);

    public sealed record CharacterWorkspaceListResponse(IReadOnlyList<CharacterWorkspaceListItemResponse> Characters, int? MainCharacterId);

    public sealed record CharacterWorkspaceListItemResponse(int CharacterId, string Name, bool IsActive, string? PortraitUrl, DateTime? LastSyncedAt, int? DkpTotal);

    public sealed record CharacterWorkspaceDetailResponse(
        CharacterWorkspaceListItemResponse Character,
        HorizonCharacterDetailResponse? Horizon,
        string? HorizonError,
        CharacterMissionProgressResponse Missions,
        CharacterDynamisClearsResponse Dynamis,
        CharacterWishlistResponse Wishlist);

    public sealed record CharacterDynamisClearsResponse(
        bool DynamisSandOria,
        bool DynamisBastok,
        bool DynamisWindurst,
        bool DynamisJeuno,
        bool DynamisBeaucedine,
        bool DynamisXarcabard,
        bool DynamisValkurm,
        bool DynamisBuburimu,
        bool DynamisQufim,
        bool DynamisTavnazia);

    public sealed record HorizonCharacterDetailResponse(
        string Name,
        int? Nation,
        string? Rank,
        bool Mentor,
        long? Settings,
        string? JobString,
        string? Avatar,
        string? PortraitUrl,
        string? SeacomType,
        string? SeacomMessage,
        bool Online,
        IReadOnlyList<HorizonJobLevelResponse> Jobs);

    public sealed record HorizonJobLevelResponse(string JobCode, int Level);

    public sealed record CharacterMissionProgressResponse(
        string? SandOriaMission,
        string? BastokMission,
        string? WindurstMission,
        string? RiseOfTheZilartMission,
        string? ChainsOfPromathiaMission,
        string? EpilogueMission,
        DateTime? UpdatedAt);

    public sealed record CharacterWishlistResponse(
        IReadOnlyList<int> SelectedItemIds,
        IReadOnlyList<CharacterWishlistItemResponse> AvailableItems,
        IReadOnlyList<CharacterWishlistAssignmentResponse> Assignments);

    public sealed record CharacterWishlistSelectionResponse(
        IReadOnlyList<int> SelectedItemIds,
        IReadOnlyList<CharacterWishlistAssignmentResponse> Assignments);

    public sealed record CharacterWishlistAssignmentResponse(int ItemId, IReadOnlyList<int> CharacterIds);

    public sealed record CharacterWishlistItemResponse(
        int ItemId,
        string Name,
        int RequiredLevel,
        string? Slot,
        string? Notes,
        IReadOnlyList<string> AllowedJobs,
        IReadOnlyList<CharacterWishlistSourceResponse> Sources);

    public sealed record CharacterWishlistContentGroupResponse(int Id, string Name, string Tag);

    public sealed record CharacterWishlistSourceResponse(int Id, string Name, string Tag, CharacterWishlistContentGroupResponse Group);

    public sealed class UpdateCharacterMissionProgressRequest
    {
        public string? SandOriaMission { get; set; }

        public string? BastokMission { get; set; }

        public string? WindurstMission { get; set; }

        public string? RiseOfTheZilartMission { get; set; }

        public string? ChainsOfPromathiaMission { get; set; }

        public string? EpilogueMission { get; set; }
    }

    public sealed class UpdateCharacterWishlistRequest
    {
        public List<int> ItemIds { get; set; } = new();

        public List<UpdateCharacterWishlistAssignmentRequest> Assignments { get; set; } = new();
    }

    public sealed class UpdateCharacterWishlistAssignmentRequest
    {
        public int ItemId { get; set; }

        public List<int> CharacterIds { get; set; } = new();
    }

    private sealed record CharacterWishlistAssignmentUpdate(int ItemId, List<int> CharacterIds);

    public sealed class UpdateDynamisClearsRequest
    {
        public bool DynamisSandOria { get; set; }
        public bool DynamisBastok { get; set; }
        public bool DynamisWindurst { get; set; }
        public bool DynamisJeuno { get; set; }
        public bool DynamisBeaucedine { get; set; }
        public bool DynamisXarcabard { get; set; }
        public bool DynamisValkurm { get; set; }
        public bool DynamisBuburimu { get; set; }
        public bool DynamisQufim { get; set; }
        public bool DynamisTavnazia { get; set; }
    }
}
