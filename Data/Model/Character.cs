using System;
using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class Character
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int OwnerUserId { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public string DataSource { get; set; } = string.Empty;

    public DateTime? LastSyncedAt { get; set; }
}
