namespace InfinityCodexWebApp.Integrations.Horizon;

public sealed class HorizonApiOptions
{
    public const string SectionName = "HorizonApi";

    public string BaseUrl { get; set; } = "https://api.horizonxi.com/";

    public string CharactersPath { get; set; } = "api/v1/chars";

    public int TimeoutSeconds { get; set; } = 10;

    public int MaxResults { get; set; } = 25;

    public int MinimumSearchLength { get; set; } = 2;

    public int MaximumSearchLength { get; set; } = 32;
}
