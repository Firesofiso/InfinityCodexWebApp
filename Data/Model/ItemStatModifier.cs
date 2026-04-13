using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class ItemStatModifier
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }

    [Required]
    public string StatKey { get; set; } = string.Empty;

    public decimal StatValue { get; set; }

    public string? Unit { get; set; }

    public int SortOrder { get; set; }
}
