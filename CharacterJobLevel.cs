using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class CharacterJobLevel
{
    public int CharacterId { get; set; }

    [Required]
    public string JobCode { get; set; } = string.Empty;

    public int Level { get; set; }
}
