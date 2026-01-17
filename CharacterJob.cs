using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class CharacterJob
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [Required]
    public string JobCode { get; set; } = string.Empty;
}
