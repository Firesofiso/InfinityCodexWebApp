using System;
using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string DiscordId { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
