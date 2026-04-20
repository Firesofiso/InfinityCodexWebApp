using System;
using System.ComponentModel.DataAnnotations;

namespace InfinityCodexWebApp;

public class Character
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int OwnerUserId { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public string DataSource { get; set; } = string.Empty;

    public string? PortraitUrl { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Mission progress
    [StringLength(128)]
    public string? SandOriaMission { get; set; }

    [StringLength(128)]
    public string? BastokMission { get; set; }

    [StringLength(128)]
    public string? WindurstMission { get; set; }

    [StringLength(128)]
    public string? RiseOfTheZilartMission { get; set; }

    [StringLength(128)]
    public string? ChainsOfPromathiaMission { get; set; }

    [StringLength(128)]
    public string? EpilogueMission { get; set; }

    // Dynamis zone clears
    public bool DynamisSandOria { get; set; }
    public bool DynamisBastok { get; set; }
    public bool DynamisWindurst { get; set; }
    public bool DynamisJeuno { get; set; }
    public bool DynamisBeaucedine { get; set; }
    public bool DynamisXarcabard { get; set; }
    public bool DynamisValkurm { get; set; }
    public bool DynamisBuburimu { get; set; }
    public bool DynamisQufim { get; set; }
    public bool DynamisTavnazia { get; set; }

    // Job levels (null = not yet synced from HorizonXI)
    public int? JobWarLevel { get; set; }
    public int? JobMnkLevel { get; set; }
    public int? JobWhmLevel { get; set; }
    public int? JobBlmLevel { get; set; }
    public int? JobRdmLevel { get; set; }
    public int? JobThfLevel { get; set; }
    public int? JobPldLevel { get; set; }
    public int? JobDrkLevel { get; set; }
    public int? JobBstLevel { get; set; }
    public int? JobBrdLevel { get; set; }
    public int? JobRngLevel { get; set; }
    public int? JobSamLevel { get; set; }
    public int? JobNinLevel { get; set; }
    public int? JobDrgLevel { get; set; }
    public int? JobSmnLevel { get; set; }

    // Craft levels (null = not yet synced from HorizonXI)
    public int? CraftSmithingLevel { get; set; }
    public int? CraftGoldsmithingLevel { get; set; }
    public int? CraftClothcraftLevel { get; set; }
    public int? CraftLeathercraftLevel { get; set; }
    public int? CraftBonecraftLevel { get; set; }
    public int? CraftWoodworkingLevel { get; set; }
    public int? CraftAlchemyLevel { get; set; }
    public int? CraftCookingLevel { get; set; }
    public int? CraftFishingLevel { get; set; }
}
