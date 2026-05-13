using System;
using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class DkpEarnEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Label { get; set; } = string.Empty;

    public int Amount { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
