using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class UserPreferredJob
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    public string JobCode { get; set; } = string.Empty;
}