using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class CharacterItemNeed
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public int ItemId { get; set; }

    [Required]
    public string State { get; set; } = string.Empty;
}
