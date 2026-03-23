using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class Item
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int RequiredLevel { get; set; }

    public string? Slot { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }
}
