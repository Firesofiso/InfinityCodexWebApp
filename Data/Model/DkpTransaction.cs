using System;
using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public static class DkpTransactionSourceTypes
{
    public const string ManualAdjustment = "manual_adjustment";
    public const string BulkEarnEvent = "bulk_earn_event";
}

public class DkpTransaction
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? CharacterId { get; set; }

    public int? EarnEventId { get; set; }

    [Required]
    [StringLength(64)]
    public string SourceType { get; set; } = DkpTransactionSourceTypes.ManualAdjustment;

    [Required]
    [StringLength(256)]
    public string Reason { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
