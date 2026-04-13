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

    public string? ImagePath { get; set; }

    public string? ItemType { get; set; }

    public bool IsRare { get; set; }

    public bool IsExclusive { get; set; }

    public string? EquipSlotGroup { get; set; }

    public string? RawEffectText { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
