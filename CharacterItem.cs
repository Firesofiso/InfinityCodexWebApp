using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class CharacterItem
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public int ItemId { get; set; }
}
