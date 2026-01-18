using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class ItemAllowedJob
{
    public int ItemId { get; set; }

    [Required]
    public string JobCode { get; set; } = string.Empty;
}
