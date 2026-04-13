namespace InfinityCodexWebApp;

public static class CatalogJobDefinitions
{
    public static readonly IReadOnlyList<CatalogJobOption> All =
    [
        new("WAR", "Warrior"),
        new("MNK", "Monk"),
        new("WHM", "White Mage"),
        new("BLM", "Black Mage"),
        new("RDM", "Red Mage"),
        new("THF", "Thief"),
        new("PLD", "Paladin"),
        new("DRK", "Dark Knight"),
        new("BST", "Beastmaster"),
        new("BRD", "Bard"),
        new("RNG", "Ranger"),
        new("SAM", "Samurai"),
        new("NIN", "Ninja"),
        new("DRG", "Dragoon"),
        new("SMN", "Summoner")
    ];

    public static readonly ISet<string> CodeSet = new HashSet<string>(
        All.Select(job => job.Code),
        StringComparer.OrdinalIgnoreCase);
}

public sealed record CatalogJobOption(string Code, string Label);