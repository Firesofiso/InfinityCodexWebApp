using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class CharacterMissionProgress
{
    [Key]
    public int CharacterId { get; set; }

    [StringLength(128)]
    public string? SanDOriaMission { get; set; }

    [StringLength(128)]
    public string? BastokMission { get; set; }

    [StringLength(128)]
    public string? WindurstMission { get; set; }

    [StringLength(128)]
    public string? RiseOfTheZilartMission { get; set; }

    [StringLength(128)]
    public string? ChainsOfPromathiaMission { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}