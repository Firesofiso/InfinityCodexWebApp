using System.Security.Claims;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Authorize]
[Route("api/dkp")]
public sealed class DkpController(ApplicationDbContext dbContext) : ControllerBase
{
    private const int MaxReasonLength = 256;
    private const int MaxEventLabelLength = 128;

    [HttpGet("characters/{characterId:int}/transactions")]
    public async Task<IActionResult> GetCharacterTransactions(
        int characterId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
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

        Character? character = await dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.IsActive, cancellationToken);

        if (character is null)
        {
            return NotFound(new { message = "Character was not found." });
        }

        bool canReadAll = HasDkpWriteAccess(currentUser);
        if (!canReadAll && character.OwnerUserId != currentUser.Id)
        {
            return Forbid();
        }

        int safeLimit = Math.Clamp(limit, 1, 200);

        List<DkpTransactionResponse> entries = await dbContext.DkpTransactions
            .Where(t => t.UserId == character.OwnerUserId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(safeLimit)
            .Select(t => new DkpTransactionResponse(
                t.Id,
                t.SourceType,
                t.Reason,
                t.Amount,
                t.BalanceAfter,
                t.CreatedAt,
                t.CharacterId,
                t.EarnEventId))
            .ToListAsync(cancellationToken);

        return Ok(new DkpTransactionListResponse(characterId, character.OwnerUserId, entries));
    }

    [HttpPost("characters/{characterId:int}/adjust")]
    public async Task<IActionResult> AdjustCharacterDkp(
        int characterId,
        [FromBody] DkpManualAdjustmentRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Adjustment payload is required." });
        }

        if (request.Amount == 0)
        {
            return BadRequest(new { message = "Amount must be a non-zero number." });
        }

        string reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(new { message = "A reason is required." });
        }

        if (reason.Length > MaxReasonLength)
        {
            return BadRequest(new { message = $"Reason must be {MaxReasonLength} characters or fewer." });
        }

        User? actor = await GetCurrentUserAsync(cancellationToken);
        if (actor is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!actor.IsActive)
        {
            return Forbid();
        }

        if (!CanManageDkp(actor))
        {
            return Forbid();
        }

        Character? character = await dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.IsActive, cancellationToken);

        if (character is null)
        {
            return NotFound(new { message = "Character was not found." });
        }

        User? targetUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == character.OwnerUserId && u.IsActive, cancellationToken);

        if (targetUser is null)
        {
            return NotFound(new { message = "Character owner was not found." });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int nextBalance = targetUser.DkpBalance + request.Amount;
        if (nextBalance < 0)
        {
            return BadRequest(new
            {
                message = "This adjustment would reduce DKP below zero.",
                currentBalance = targetUser.DkpBalance,
                maxDeductible = targetUser.DkpBalance
            });
        }

        targetUser.DkpBalance = nextBalance;

        DkpTransaction ledgerEntry = new()
        {
            UserId = targetUser.Id,
            CharacterId = character.Id,
            SourceType = DkpTransactionSourceTypes.ManualAdjustment,
            Reason = reason,
            Amount = request.Amount,
            BalanceAfter = nextBalance,
            CreatedByUserId = actor.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.DkpTransactions.Add(ledgerEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new DkpAdjustmentResponse(
            character.Id,
            targetUser.Id,
            targetUser.DkpBalance,
            new DkpTransactionResponse(
                ledgerEntry.Id,
                ledgerEntry.SourceType,
                ledgerEntry.Reason,
                ledgerEntry.Amount,
                ledgerEntry.BalanceAfter,
                ledgerEntry.CreatedAt,
                ledgerEntry.CharacterId,
                ledgerEntry.EarnEventId)));
    }

    [HttpPost("events/earn")]
    public async Task<IActionResult> CreateBulkEarnEvent(
        [FromBody] DkpBulkEarnRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Earn event payload is required." });
        }

        string label = request.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(label))
        {
            return BadRequest(new { message = "An event label is required." });
        }

        if (label.Length > MaxEventLabelLength)
        {
            return BadRequest(new { message = $"Event label must be {MaxEventLabelLength} characters or fewer." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Earn amount must be greater than zero." });
        }

        int[] requestedCharacterIds = request.CharacterIds
            .Distinct()
            .ToArray();

        if (requestedCharacterIds.Length == 0)
        {
            return BadRequest(new { message = "At least one character is required." });
        }

        User? actor = await GetCurrentUserAsync(cancellationToken);
        if (actor is null)
        {
            return Unauthorized(new { message = "User session could not be resolved." });
        }

        if (!actor.IsActive)
        {
            return Forbid();
        }

        if (!CanManageDkp(actor))
        {
            return Forbid();
        }

        List<Character> characters = await dbContext.Characters
            .Where(c => requestedCharacterIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(cancellationToken);

        int[] foundCharacterIds = characters.Select(c => c.Id).ToArray();
        int[] missingCharacterIds = requestedCharacterIds
            .Except(foundCharacterIds)
            .ToArray();

        if (missingCharacterIds.Length > 0)
        {
            return BadRequest(new
            {
                message = "One or more characters were not found.",
                missingCharacterIds
            });
        }

        int[] targetUserIds = characters
            .Select(c => c.OwnerUserId)
            .Distinct()
            .ToArray();

        List<User> targetUsers = await dbContext.Users
            .Where(u => targetUserIds.Contains(u.Id) && u.IsActive)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        if (targetUsers.Count != targetUserIds.Length)
        {
            return BadRequest(new { message = "One or more character owners are inactive or missing." });
        }

        DateTime occurredAt = request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow;

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        DkpEarnEvent earnEvent = new()
        {
            Label = label,
            Amount = request.Amount,
            OccurredAt = occurredAt,
            CreatedByUserId = actor.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.DkpEarnEvents.Add(earnEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTime entryCreatedAt = DateTime.UtcNow;
        List<DkpTransaction> ledgerEntries = new(targetUsers.Count);
        foreach (User targetUser in targetUsers)
        {
            targetUser.DkpBalance += request.Amount;

            DkpTransaction ledgerEntry = new()
            {
                UserId = targetUser.Id,
                SourceType = DkpTransactionSourceTypes.BulkEarnEvent,
                Reason = label,
                Amount = request.Amount,
                BalanceAfter = targetUser.DkpBalance,
                CreatedByUserId = actor.Id,
                EarnEventId = earnEvent.Id,
                CreatedAt = entryCreatedAt
            };

            ledgerEntries.Add(ledgerEntry);
        }

        dbContext.DkpTransactions.AddRange(ledgerEntries);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        IReadOnlyList<DkpTransactionResponse> entries = ledgerEntries
            .Select(ledgerEntry => new DkpTransactionResponse(
                ledgerEntry.Id,
                ledgerEntry.SourceType,
                ledgerEntry.Reason,
                ledgerEntry.Amount,
                ledgerEntry.BalanceAfter,
                ledgerEntry.CreatedAt,
                ledgerEntry.CharacterId,
                ledgerEntry.EarnEventId))
            .ToList();

        return Ok(new DkpBulkEarnResponse(
            earnEvent.Id,
            earnEvent.Label,
            earnEvent.Amount,
            earnEvent.OccurredAt,
            targetUsers.Count,
            entries));
    }

    private bool HasDkpWriteAccess(User user)
    {
        return user.IsActive
            && RolePermissions.RoleHasPermission(user.Role, AppPermissions.ManageDkp);
    }

    private bool CanManageDkp(User user)
    {
        return HasDkpWriteAccess(user);
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

    public sealed class DkpManualAdjustmentRequest
    {
        public int Amount { get; set; }

        public string? Reason { get; set; }
    }

    public sealed class DkpBulkEarnRequest
    {
        public string? Label { get; set; }

        public int Amount { get; set; }

        public List<int> CharacterIds { get; set; } = new();

        public DateTime? OccurredAt { get; set; }
    }

    public sealed record DkpAdjustmentResponse(
        int CharacterId,
        int UserId,
        int NewBalance,
        DkpTransactionResponse Transaction);

    public sealed record DkpBulkEarnResponse(
        int EventId,
        string Label,
        int Amount,
        DateTime OccurredAt,
        int AffectedMemberCount,
        IReadOnlyList<DkpTransactionResponse> Transactions);

    public sealed record DkpTransactionListResponse(
        int CharacterId,
        int UserId,
        IReadOnlyList<DkpTransactionResponse> Entries);

    public sealed record DkpTransactionResponse(
        int Id,
        string SourceType,
        string Reason,
        int Amount,
        int BalanceAfter,
        DateTime CreatedAt,
        int? CharacterId,
        int? EarnEventId);
}
