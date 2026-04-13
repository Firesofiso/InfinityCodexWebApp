using System.Security.Claims;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Authorize]
[Route("api/items")]
public sealed class ItemsController(ApplicationDbContext dbContext) : ControllerBase
{
    private const int NameMaxLength = 200;
    private const int SlotMaxLength = 100;
    private const int NotesMaxLength = 2000;
    private const int ImagePathMaxLength = 500;
    private const int ItemTypeMaxLength = 50;
    private const int EquipSlotGroupMaxLength = 100;
    private const int EffectTextMaxLength = 2000;
    private const int ModifierKeyMaxLength = 64;
    private const int ModifierUnitMaxLength = 32;
    private const int MaxLevel = 75;
    private static readonly HashSet<string> AllowedItemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "armor",
        "weapon",
        "accessory",
        "consumable",
        "other"
    };

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? slot,
        [FromQuery] int[]? sourceIds,
        CancellationToken cancellationToken)
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

        var normalizedSearch = NormalizeSearch(search);
        var normalizedSlot = NormalizeText(slot);
        var requestedSourceIds = (sourceIds ?? []).Where(id => id > 0).Distinct().ToArray();

        var itemsQuery = dbContext.Items.AsNoTracking();
        if (isActive.HasValue)
        {
            itemsQuery = itemsQuery.Where(item => item.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            itemsQuery = itemsQuery.Where(item =>
                EF.Functions.Like(item.Name, $"%{normalizedSearch}%")
                || (item.Slot != null && EF.Functions.Like(item.Slot, $"%{normalizedSearch}%"))
                || (item.Notes != null && EF.Functions.Like(item.Notes, $"%{normalizedSearch}%")));
        }

        if (!string.IsNullOrWhiteSpace(normalizedSlot))
        {
            itemsQuery = itemsQuery.Where(item => item.Slot != null && item.Slot == normalizedSlot);
        }

        if (requestedSourceIds.Length > 0)
        {
            itemsQuery = itemsQuery.Where(item => dbContext.ItemSources.Any(itemSource => itemSource.ItemId == item.Id && requestedSourceIds.Contains(itemSource.ContentSourceId)));
        }

        var itemsTask = itemsQuery
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var allowedJobsTask = dbContext.ItemAllowedJobs.AsNoTracking().ToListAsync(cancellationToken);
        var armorStatsTask = dbContext.ItemArmorStats.AsNoTracking().ToListAsync(cancellationToken);
        var weaponStatsTask = dbContext.ItemWeaponStats.AsNoTracking().ToListAsync(cancellationToken);
        var accessoryStatsTask = dbContext.ItemAccessoryStats.AsNoTracking().ToListAsync(cancellationToken);
        var modifiersTask = dbContext.ItemStatModifiers.AsNoTracking().ToListAsync(cancellationToken);
        var itemSourcesTask = (from itemSource in dbContext.ItemSources.AsNoTracking()
                               join contentSource in dbContext.ContentSources.AsNoTracking()
                                   on itemSource.ContentSourceId equals contentSource.Id
                               join contentGroup in dbContext.ContentGroups.AsNoTracking()
                                   on contentSource.ContentGroupId equals contentGroup.Id
                               select new ItemSourceLookup(
                                   itemSource.ItemId,
                                   contentSource.Id,
                                   contentSource.Tag,
                                   contentSource.Name,
                                   contentGroup.Id,
                                   contentGroup.Tag,
                                   contentGroup.Name))
            .ToListAsync(cancellationToken);

        await Task.WhenAll(itemsTask, allowedJobsTask, armorStatsTask, weaponStatsTask, accessoryStatsTask, modifiersTask, itemSourcesTask);

        var allowedJobsByItem = allowedJobsTask.Result
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(row => row.JobCode.ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(jobCode => jobCode)
                    .ToList());

        var sourcesByItem = itemSourcesTask.Result
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ContentSourceReferenceResponse>)group
                    .OrderBy(row => row.GroupTag)
                    .ThenBy(row => string.IsNullOrWhiteSpace(row.Tag) ? row.Name : row.Tag)
                    .Select(row => new ContentSourceReferenceResponse(
                        row.Id,
                        row.Name,
                        row.Tag,
                        new ContentGroupReferenceResponse(row.GroupId, row.GroupName, row.GroupTag)))
                    .ToList());

        var armorByItem = armorStatsTask.Result
            .ToDictionary(
                row => row.ItemId,
                row => new ItemArmorStatsResponse(row.Defense, row.MagicDefense));

        var weaponByItem = weaponStatsTask.Result
            .ToDictionary(
                row => row.ItemId,
                row => new ItemWeaponStatsResponse(row.Damage, row.Delay, row.Dps));

        var accessoryByItem = accessoryStatsTask.Result
            .ToDictionary(
                row => row.ItemId,
                row => new ItemAccessoryStatsResponse(row.Charges, row.RecastSeconds));

        var modifiersByItem = modifiersTask.Result
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemStatModifierResponse>)group
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.StatKey)
                    .Select(row => new ItemStatModifierResponse(row.StatKey, row.StatValue, row.Unit, row.SortOrder))
                    .ToList());

        var items = itemsTask.Result
            .Select(item => new CatalogItemSummaryResponse(
                item.Id,
                item.Name,
                item.RequiredLevel,
                item.Slot,
                item.Notes,
                item.ImagePath,
                item.ItemType,
                item.IsRare,
                item.IsExclusive,
                item.EquipSlotGroup,
                item.RawEffectText,
                item.IsActive,
                item.UpdatedAt,
                armorByItem.GetValueOrDefault(item.Id),
                weaponByItem.GetValueOrDefault(item.Id),
                accessoryByItem.GetValueOrDefault(item.Id),
                modifiersByItem.GetValueOrDefault(item.Id, Array.Empty<ItemStatModifierResponse>()),
                allowedJobsByItem.GetValueOrDefault(item.Id, Array.Empty<string>()),
                sourcesByItem.GetValueOrDefault(item.Id, Array.Empty<ContentSourceReferenceResponse>())))
            .ToList();

        return Ok(new CatalogItemListResponse(items));
    }

    [HttpGet("{itemId:int}")]
    public async Task<IActionResult> GetItem(int itemId, CancellationToken cancellationToken)
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

        var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == itemId, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Catalog item was not found." });
        }

        return Ok(await BuildDetailResponseAsync(item.Id, cancellationToken));
    }

    [HttpGet("reference-data")]
    public async Task<IActionResult> GetReferenceData(CancellationToken cancellationToken)
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

        var sources = await (from source in dbContext.ContentSources.AsNoTracking()
                             join contentGroup in dbContext.ContentGroups.AsNoTracking()
                                 on source.ContentGroupId equals contentGroup.Id
                             orderby source.IsActive descending, contentGroup.Tag, source.Tag, source.Name
                             select new ContentSourceResponse(
                                 source.Id,
                                 source.Name,
                                 source.Tag,
                                 source.IsActive,
                                 source.Notes,
                                 new ContentGroupReferenceResponse(contentGroup.Id, contentGroup.Name, contentGroup.Tag)))
            .ToListAsync(cancellationToken);

        return Ok(new CatalogReferenceDataResponse(CatalogJobDefinitions.All, sources));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<IActionResult> CreateItem([FromBody] UpsertCatalogItemRequest? request, CancellationToken cancellationToken)
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

        var validationError = await ValidateRequestAsync(request, null, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedName = request!.Name!.Trim();
        var normalizedSlot = NormalizeNullable(request.Slot);
        var normalizedNotes = NormalizeNullable(request.Notes);
        var normalizedImagePath = NormalizeNullable(request.ImagePath);
        var normalizedItemType = NormalizeItemType(request.ItemType);
        var normalizedEquipSlotGroup = NormalizeNullable(request.EquipSlotGroup);
        var normalizedEffectText = NormalizeNullable(request.RawEffectText);
        var normalizedJobs = NormalizeJobs(request.AllowedJobs);
        var normalizedSourceIds = NormalizeSourceIds(request.SourceIds);

        var item = new Item
        {
            Name = normalizedName,
            RequiredLevel = request.RequiredLevel ?? 0,
            Slot = normalizedSlot,
            Notes = normalizedNotes,
            ImagePath = normalizedImagePath,
            ItemType = normalizedItemType,
            IsRare = request.IsRare,
            IsExclusive = request.IsExclusive,
            EquipSlotGroup = normalizedEquipSlotGroup,
            RawEffectText = normalizedEffectText,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceMappingsAsync(
            item.Id,
            normalizedJobs,
            normalizedSourceIds,
            request.ArmorStats,
            request.WeaponStats,
            request.AccessoryStats,
            request.Modifiers,
            cancellationToken);

        return CreatedAtAction(nameof(GetItem), new { itemId = item.Id }, await BuildDetailResponseAsync(item.Id, cancellationToken));
    }

    [HttpPut("{itemId:int}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpsertCatalogItemRequest? request, CancellationToken cancellationToken)
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

        var item = await dbContext.Items.FirstOrDefaultAsync(entity => entity.Id == itemId, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Catalog item was not found." });
        }

        var validationError = await ValidateRequestAsync(request, itemId, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        item.Name = request!.Name!.Trim();
        item.RequiredLevel = request.RequiredLevel ?? 0;
        item.Slot = NormalizeNullable(request.Slot);
        item.Notes = NormalizeNullable(request.Notes);
        item.ImagePath = NormalizeNullable(request.ImagePath);
        item.ItemType = NormalizeItemType(request.ItemType);
        item.IsRare = request.IsRare;
        item.IsExclusive = request.IsExclusive;
        item.EquipSlotGroup = NormalizeNullable(request.EquipSlotGroup);
        item.RawEffectText = NormalizeNullable(request.RawEffectText);
        item.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await ReplaceMappingsAsync(
            item.Id,
            NormalizeJobs(request.AllowedJobs),
            NormalizeSourceIds(request.SourceIds),
            request.ArmorStats,
            request.WeaponStats,
            request.AccessoryStats,
            request.Modifiers,
            cancellationToken);

        return Ok(await BuildDetailResponseAsync(item.Id, cancellationToken));
    }

    [HttpPost("{itemId:int}/archive")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<IActionResult> SetItemArchived(int itemId, [FromBody] ArchiveCatalogItemRequest? request, CancellationToken cancellationToken)
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

        var item = await dbContext.Items.FirstOrDefaultAsync(entity => entity.Id == itemId, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Catalog item was not found." });
        }

        item.IsActive = !(request?.Archived ?? true);
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await BuildDetailResponseAsync(item.Id, cancellationToken));
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

    private async Task<IActionResult?> ValidateRequestAsync(UpsertCatalogItemRequest? request, int? existingItemId, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Catalog item payload is required." });
        }

        var normalizedName = NormalizeNullable(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest(new { message = "Item name is required.", field = "name" });
        }

        if (normalizedName.Length > NameMaxLength)
        {
            return BadRequest(new { message = $"Item name must be {NameMaxLength} characters or fewer.", field = "name" });
        }

        var normalizedSlot = NormalizeNullable(request.Slot);
        if (normalizedSlot is { Length: > SlotMaxLength })
        {
            return BadRequest(new { message = $"Slot/category must be {SlotMaxLength} characters or fewer.", field = "slot" });
        }

        var normalizedNotes = NormalizeNullable(request.Notes);
        if (normalizedNotes is { Length: > NotesMaxLength })
        {
            return BadRequest(new { message = $"Notes must be {NotesMaxLength} characters or fewer.", field = "notes" });
        }

        var normalizedImagePath = NormalizeNullable(request.ImagePath);
        if (normalizedImagePath is { Length: > ImagePathMaxLength })
        {
            return BadRequest(new { message = $"Image path must be {ImagePathMaxLength} characters or fewer.", field = "imagePath" });
        }

        var normalizedItemType = NormalizeItemType(request.ItemType);
        if (normalizedItemType is { Length: > ItemTypeMaxLength })
        {
            return BadRequest(new { message = $"Item type must be {ItemTypeMaxLength} characters or fewer.", field = "itemType" });
        }

        if (!string.IsNullOrWhiteSpace(normalizedItemType) && !AllowedItemTypes.Contains(normalizedItemType))
        {
            return BadRequest(new { message = "Item type is invalid.", field = "itemType" });
        }

        var normalizedEquipSlotGroup = NormalizeNullable(request.EquipSlotGroup);
        if (normalizedEquipSlotGroup is { Length: > EquipSlotGroupMaxLength })
        {
            return BadRequest(new { message = $"Equip slot group must be {EquipSlotGroupMaxLength} characters or fewer.", field = "equipSlotGroup" });
        }

        var normalizedEffectText = NormalizeNullable(request.RawEffectText);
        if (normalizedEffectText is { Length: > EffectTextMaxLength })
        {
            return BadRequest(new { message = $"Raw effect text must be {EffectTextMaxLength} characters or fewer.", field = "rawEffectText" });
        }

        if (request.ArmorStats is { Defense: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Armor defense must be between 0 and 9999.", field = "armorStats.defense" });
        }

        if (request.ArmorStats is { MagicDefense: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Armor magic defense must be between 0 and 9999.", field = "armorStats.magicDefense" });
        }

        if (request.WeaponStats is { Damage: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Weapon damage must be between 0 and 9999.", field = "weaponStats.damage" });
        }

        if (request.WeaponStats is { Delay: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Weapon delay must be between 0 and 9999.", field = "weaponStats.delay" });
        }

        if (request.WeaponStats is { Dps: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Weapon DPS must be between 0 and 9999.", field = "weaponStats.dps" });
        }

        if (request.AccessoryStats is { Charges: < 0 or > 9999 })
        {
            return BadRequest(new { message = "Accessory charges must be between 0 and 9999.", field = "accessoryStats.charges" });
        }

        if (request.AccessoryStats is { RecastSeconds: < 0 or > 86400 })
        {
            return BadRequest(new { message = "Accessory recast seconds must be between 0 and 86400.", field = "accessoryStats.recastSeconds" });
        }

        foreach (var modifier in (request.Modifiers ?? []).Where(row => row is not null))
        {
            var statKey = NormalizeNullable(modifier.StatKey);
            if (string.IsNullOrWhiteSpace(statKey))
            {
                return BadRequest(new { message = "Modifier stat key is required.", field = "modifiers.statKey" });
            }

            if (statKey.Length > ModifierKeyMaxLength)
            {
                return BadRequest(new { message = $"Modifier stat key must be {ModifierKeyMaxLength} characters or fewer.", field = "modifiers.statKey" });
            }

            if (modifier.StatValue is < -9999 or > 9999)
            {
                return BadRequest(new { message = "Modifier stat value must be between -9999 and 9999.", field = "modifiers.statValue" });
            }

            var unit = NormalizeNullable(modifier.Unit);
            if (unit is { Length: > ModifierUnitMaxLength })
            {
                return BadRequest(new { message = $"Modifier unit must be {ModifierUnitMaxLength} characters or fewer.", field = "modifiers.unit" });
            }
        }

        if (request.RequiredLevel is < 0 or > MaxLevel)
        {
            return BadRequest(new { message = $"Required level must be between 0 and {MaxLevel}.", field = "requiredLevel" });
        }

        var normalizedJobs = NormalizeJobs(request.AllowedJobs);
        if (normalizedJobs.Count > 0 && normalizedJobs.Any(jobCode => !CatalogJobDefinitions.CodeSet.Contains(jobCode)))
        {
            return BadRequest(new { message = "One or more job codes are invalid.", field = "allowedJobs" });
        }

        var normalizedSourceIds = NormalizeSourceIds(request.SourceIds);
        if (normalizedSourceIds.Count > 0)
        {
            var validSourceCount = await dbContext.ContentSources.AsNoTracking()
                .CountAsync(source => normalizedSourceIds.Contains(source.Id), cancellationToken);

            if (validSourceCount != normalizedSourceIds.Count)
            {
                return BadRequest(new { message = "One or more content sources are invalid.", field = "sourceIds" });
            }
        }

        var normalizedNameKey = NormalizeKey(normalizedName);
        var normalizedSlotKey = NormalizeKey(normalizedSlot);
        var conflictingItems = await dbContext.Items.AsNoTracking()
            .Where(item => item.IsActive && (!existingItemId.HasValue || item.Id != existingItemId.Value))
            .ToListAsync(cancellationToken);

        var hasDuplicate = conflictingItems.Any(item =>
            NormalizeKey(item.Name) == normalizedNameKey && NormalizeKey(item.Slot) == normalizedSlotKey);

        if (hasDuplicate)
        {
            return Conflict(new { message = "An active catalog item with the same name and slot/category already exists.", field = "name" });
        }

        return null;
    }

    private async Task ReplaceMappingsAsync(
        int itemId,
        IReadOnlyList<string> allowedJobs,
        IReadOnlyList<int> sourceIds,
        UpsertItemArmorStatsRequest? armorStats,
        UpsertItemWeaponStatsRequest? weaponStats,
        UpsertItemAccessoryStatsRequest? accessoryStats,
        IReadOnlyList<UpsertItemStatModifierRequest> modifiers,
        CancellationToken cancellationToken)
    {
        var existingJobs = await dbContext.ItemAllowedJobs.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingJobs.Count > 0)
        {
            dbContext.ItemAllowedJobs.RemoveRange(existingJobs);
        }

        var existingSources = await dbContext.ItemSources.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingSources.Count > 0)
        {
            dbContext.ItemSources.RemoveRange(existingSources);
        }

        var existingArmorStats = await dbContext.ItemArmorStats.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingArmorStats.Count > 0)
        {
            dbContext.ItemArmorStats.RemoveRange(existingArmorStats);
        }

        var existingWeaponStats = await dbContext.ItemWeaponStats.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingWeaponStats.Count > 0)
        {
            dbContext.ItemWeaponStats.RemoveRange(existingWeaponStats);
        }

        var existingAccessoryStats = await dbContext.ItemAccessoryStats.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingAccessoryStats.Count > 0)
        {
            dbContext.ItemAccessoryStats.RemoveRange(existingAccessoryStats);
        }

        var existingModifiers = await dbContext.ItemStatModifiers.Where(entity => entity.ItemId == itemId).ToListAsync(cancellationToken);
        if (existingModifiers.Count > 0)
        {
            dbContext.ItemStatModifiers.RemoveRange(existingModifiers);
        }

        foreach (var jobCode in allowedJobs)
        {
            dbContext.ItemAllowedJobs.Add(new ItemAllowedJob
            {
                ItemId = itemId,
                JobCode = jobCode
            });
        }

        foreach (var sourceId in sourceIds)
        {
            dbContext.ItemSources.Add(new ItemSource
            {
                ItemId = itemId,
                ContentSourceId = sourceId
            });
        }

        if (armorStats is not null)
        {
            dbContext.ItemArmorStats.Add(new ItemArmorStats
            {
                ItemId = itemId,
                Defense = armorStats.Defense,
                MagicDefense = armorStats.MagicDefense
            });
        }

        if (weaponStats is not null)
        {
            dbContext.ItemWeaponStats.Add(new ItemWeaponStats
            {
                ItemId = itemId,
                Damage = weaponStats.Damage,
                Delay = weaponStats.Delay,
                Dps = weaponStats.Dps
            });
        }

        if (accessoryStats is not null)
        {
            dbContext.ItemAccessoryStats.Add(new ItemAccessoryStats
            {
                ItemId = itemId,
                Charges = accessoryStats.Charges,
                RecastSeconds = accessoryStats.RecastSeconds
            });
        }

        foreach (var modifier in (modifiers ?? []).Where(row => row is not null))
        {
            var key = NormalizeNullable(modifier.StatKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            dbContext.ItemStatModifiers.Add(new ItemStatModifier
            {
                ItemId = itemId,
                StatKey = key,
                StatValue = modifier.StatValue,
                Unit = NormalizeNullable(modifier.Unit),
                SortOrder = modifier.SortOrder ?? 0
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CatalogItemDetailResponse> BuildDetailResponseAsync(int itemId, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.AsNoTracking().FirstAsync(entity => entity.Id == itemId, cancellationToken);
        var allowedJobs = await dbContext.ItemAllowedJobs.AsNoTracking()
            .Where(entity => entity.ItemId == itemId)
            .OrderBy(entity => entity.JobCode)
            .Select(entity => entity.JobCode.ToUpper())
            .ToListAsync(cancellationToken);

        var sources = await (from itemSource in dbContext.ItemSources.AsNoTracking()
                             join contentSource in dbContext.ContentSources.AsNoTracking()
                                 on itemSource.ContentSourceId equals contentSource.Id
                             join contentGroup in dbContext.ContentGroups.AsNoTracking()
                                 on contentSource.ContentGroupId equals contentGroup.Id
                             where itemSource.ItemId == itemId
                             orderby contentGroup.Tag, contentSource.Tag, contentSource.Name
                             select new ContentSourceReferenceResponse(
                                 contentSource.Id,
                                 contentSource.Name,
                                 contentSource.Tag,
                                 new ContentGroupReferenceResponse(contentGroup.Id, contentGroup.Name, contentGroup.Tag)))
            .ToListAsync(cancellationToken);

        var armorStats = await dbContext.ItemArmorStats.AsNoTracking()
            .Where(entity => entity.ItemId == itemId)
            .Select(entity => new ItemArmorStatsResponse(entity.Defense, entity.MagicDefense))
            .FirstOrDefaultAsync(cancellationToken);

        var weaponStats = await dbContext.ItemWeaponStats.AsNoTracking()
            .Where(entity => entity.ItemId == itemId)
            .Select(entity => new ItemWeaponStatsResponse(entity.Damage, entity.Delay, entity.Dps))
            .FirstOrDefaultAsync(cancellationToken);

        var accessoryStats = await dbContext.ItemAccessoryStats.AsNoTracking()
            .Where(entity => entity.ItemId == itemId)
            .Select(entity => new ItemAccessoryStatsResponse(entity.Charges, entity.RecastSeconds))
            .FirstOrDefaultAsync(cancellationToken);

        var modifiers = await dbContext.ItemStatModifiers.AsNoTracking()
            .Where(entity => entity.ItemId == itemId)
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.StatKey)
            .Select(entity => new ItemStatModifierResponse(entity.StatKey, entity.StatValue, entity.Unit, entity.SortOrder))
            .ToListAsync(cancellationToken);

        return new CatalogItemDetailResponse(
            item.Id,
            item.Name,
            item.RequiredLevel,
            item.Slot,
            item.Notes,
            item.ImagePath,
            item.ItemType,
            item.IsRare,
            item.IsExclusive,
            item.EquipSlotGroup,
            item.RawEffectText,
            item.IsActive,
            item.UpdatedAt,
            armorStats,
            weaponStats,
            accessoryStats,
            modifiers,
            allowedJobs,
            sources);
    }

    private static string NormalizeSearch(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeText(string? value) => NormalizeNullable(value) ?? string.Empty;

    private static string? NormalizeItemType(string? value)
    {
        var normalized = NormalizeNullable(value);
        return normalized?.ToLowerInvariant();
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IReadOnlyList<string> NormalizeJobs(IEnumerable<string>? jobs)
    {
        return (jobs ?? [])
            .Select(job => job.Trim().ToUpperInvariant())
            .Where(job => !string.IsNullOrWhiteSpace(job))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(job => job)
            .ToList();
    }

    private static IReadOnlyList<int> NormalizeSourceIds(IEnumerable<int>? sourceIds)
    {
        return (sourceIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    private sealed record ItemSourceLookup(int ItemId, int Id, string Tag, string Name, int GroupId, string GroupTag, string GroupName);

    public sealed record CatalogItemListResponse(IReadOnlyList<CatalogItemSummaryResponse> Items);

    public sealed record CatalogItemSummaryResponse(
        int Id,
        string Name,
        int RequiredLevel,
        string? Slot,
        string? Notes,
        string? ImagePath,
        string? ItemType,
        bool IsRare,
        bool IsExclusive,
        string? EquipSlotGroup,
        string? RawEffectText,
        bool IsActive,
        DateTime UpdatedAt,
        ItemArmorStatsResponse? ArmorStats,
        ItemWeaponStatsResponse? WeaponStats,
        ItemAccessoryStatsResponse? AccessoryStats,
        IReadOnlyList<ItemStatModifierResponse> Modifiers,
        IReadOnlyList<string> AllowedJobs,
        IReadOnlyList<ContentSourceReferenceResponse> Sources);

    public sealed record CatalogItemDetailResponse(
        int Id,
        string Name,
        int RequiredLevel,
        string? Slot,
        string? Notes,
        string? ImagePath,
        string? ItemType,
        bool IsRare,
        bool IsExclusive,
        string? EquipSlotGroup,
        string? RawEffectText,
        bool IsActive,
        DateTime UpdatedAt,
        ItemArmorStatsResponse? ArmorStats,
        ItemWeaponStatsResponse? WeaponStats,
        ItemAccessoryStatsResponse? AccessoryStats,
        IReadOnlyList<ItemStatModifierResponse> Modifiers,
        IReadOnlyList<string> AllowedJobs,
        IReadOnlyList<ContentSourceReferenceResponse> Sources);

    public sealed record ItemArmorStatsResponse(int? Defense, int? MagicDefense);

    public sealed record ItemWeaponStatsResponse(int? Damage, int? Delay, decimal? Dps);

    public sealed record ItemAccessoryStatsResponse(int? Charges, int? RecastSeconds);

    public sealed record ItemStatModifierResponse(string StatKey, decimal StatValue, string? Unit, int SortOrder);

    public sealed record ContentGroupReferenceResponse(int Id, string Name, string Tag);

    public sealed record ContentSourceResponse(int Id, string Name, string Tag, bool IsActive, string Notes, ContentGroupReferenceResponse Group);

    public sealed record ContentSourceReferenceResponse(int Id, string Name, string Tag, ContentGroupReferenceResponse Group);

    public sealed record CatalogReferenceDataResponse(IReadOnlyList<CatalogJobOption> Jobs, IReadOnlyList<ContentSourceResponse> Sources);

    public sealed class UpsertCatalogItemRequest
    {
        public string? Name { get; set; }

        public int? RequiredLevel { get; set; }

        public string? Slot { get; set; }

        public string? Notes { get; set; }

        public string? ImagePath { get; set; }

        public string? ItemType { get; set; }

        public bool IsRare { get; set; }

        public bool IsExclusive { get; set; }

        public string? EquipSlotGroup { get; set; }

        public string? RawEffectText { get; set; }

        public UpsertItemArmorStatsRequest? ArmorStats { get; set; }

        public UpsertItemWeaponStatsRequest? WeaponStats { get; set; }

        public UpsertItemAccessoryStatsRequest? AccessoryStats { get; set; }

        public List<UpsertItemStatModifierRequest> Modifiers { get; set; } = new();

        public List<string> AllowedJobs { get; set; } = new();

        public List<int> SourceIds { get; set; } = new();
    }

    public sealed class UpsertItemArmorStatsRequest
    {
        public int? Defense { get; set; }

        public int? MagicDefense { get; set; }
    }

    public sealed class UpsertItemWeaponStatsRequest
    {
        public int? Damage { get; set; }

        public int? Delay { get; set; }

        public decimal? Dps { get; set; }
    }

    public sealed class UpsertItemAccessoryStatsRequest
    {
        public int? Charges { get; set; }

        public int? RecastSeconds { get; set; }
    }

    public sealed class UpsertItemStatModifierRequest
    {
        public string? StatKey { get; set; }

        public decimal StatValue { get; set; }

        public string? Unit { get; set; }

        public int? SortOrder { get; set; }
    }

    public sealed class ArchiveCatalogItemRequest
    {
        public bool Archived { get; set; } = true;
    }
}