using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class ContentSource
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Tag { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
