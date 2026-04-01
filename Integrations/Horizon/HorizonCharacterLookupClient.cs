using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InfinityCodexWebApp.Integrations.Horizon;

public interface IHorizonCharacterLookupClient
{
    Task<IReadOnlyList<HorizonCharacterResult>> SearchCharactersAsync(string search, CancellationToken cancellationToken);

    Task<HorizonCharacterDetailResult?> GetCharacterAsync(string characterName, CancellationToken cancellationToken);
}

public sealed record HorizonCharacterResult(
    string CharacterId,
    string Name,
    string CurrentJob,
    string? PortraitUrl,
    string? RawJobString);

public sealed record HorizonCharacterDetailResult(
    string Name,
    int? Nation,
    string? Rank,
    bool Mentor,
    long? Settings,
    string? JobString,
    string? Avatar,
    string? PortraitUrl,
    string? SeacomType,
    string? SeacomMessage,
    bool Online,
    IReadOnlyDictionary<string, int> Jobs);

public sealed class HorizonCharacterLookupClient(
    HttpClient httpClient,
    IOptions<HorizonApiOptions> options,
    ILogger<HorizonCharacterLookupClient> logger) : IHorizonCharacterLookupClient
{
    private const string HorizonPortraitBaseUrl = "https://pub-8d18c77b6a6c43f2ae9fc4c782ef9b78.r2.dev/images/account/create-character/face/";

    // Not super stoked about this class.  It seems like we have a custom way of parsing json but we could just use newtonsoft or something similar to deserialize into a model
    public async Task<IReadOnlyList<HorizonCharacterResult>> SearchCharactersAsync(string search, CancellationToken cancellationToken)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(search))
        {
            return [];
        }

        var requestUri = BuildSearchUri(config.CharactersPath, search);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Horizon search failed with {StatusCode}. Body: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"Horizon API returned {(int)response.StatusCode}", null, response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ParseResults(json, config.MaxResults);
    }

    public async Task<HorizonCharacterDetailResult?> GetCharacterAsync(string characterName, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var normalizedCharacterName = characterName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCharacterName))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildCharacterUri(config.CharactersPath, normalizedCharacterName));
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Horizon detail lookup failed with {StatusCode}. Body: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"Horizon API returned {(int)response.StatusCode}", null, response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ParseCharacterDetail(json.RootElement);
    }

    private static string BuildSearchUri(string charactersPath, string search)
    {
        var separator = charactersPath.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{charactersPath}{separator}search={Uri.EscapeDataString(search)}";
    }

    private static string BuildCharacterUri(string charactersPath, string characterName)
    {
        var normalizedPath = charactersPath.TrimEnd('/');
        return $"{normalizedPath}/{Uri.EscapeDataString(characterName)}";
    }

    private static IReadOnlyList<HorizonCharacterResult> ParseResults(JsonDocument document, int maxResults)
    {
        var nodes = GetResultArray(document.RootElement);
        var results = new List<HorizonCharacterResult>();

        foreach (var node in nodes)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(node, "charname")
                ?? GetString(node, "name")
                ?? GetString(node, "characterName");

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var jobString = GetString(node, "jobString")
                ?? GetString(node, "currentJob")
                ?? GetString(node, "job")
                ?? "Unknown";

            var id = GetString(node, "id")
                ?? GetString(node, "characterId")
                ?? name;

            var portraitUrl = ResolvePortraitUrl(node);

            results.Add(new HorizonCharacterResult(id, name, jobString, portraitUrl, jobString));

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        return results;
    }

    private static string? ResolvePortraitUrl(JsonElement node)
    {
        var portraitUrl = GetString(node, "portraitUrl")
            ?? GetString(node, "avatar")
            ?? GetString(node, "avatarUrl")
            ?? GetString(node, "imageUrl");

        if (!string.IsNullOrWhiteSpace(portraitUrl))
        {
            var normalizedUrl = portraitUrl.Trim();
            if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var parsedUri)
                && (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
            {
                return normalizedUrl;
            }

            var derivedPortraitUrl = BuildPortraitUrlFromName(normalizedUrl);
            if (!string.IsNullOrWhiteSpace(derivedPortraitUrl))
            {
                return derivedPortraitUrl;
            }
        }

        var portraitName = GetString(node, "portrait")
            ?? GetString(node, "portraitName")
            ?? GetString(node, "face")
            ?? GetString(node, "faceName")
            ?? GetString(node, "characterFace");

        return BuildPortraitUrlFromName(portraitName);
    }

    private static HorizonCharacterDetailResult? ParseCharacterDetail(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = GetString(root, "charname")
            ?? GetString(root, "name")
            ?? GetString(root, "characterName");

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var jobs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("jobs", out var jobsNode) && jobsNode.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in jobsNode.EnumerateObject())
            {
                if (TryGetInt32(property.Value, out var level))
                {
                    jobs[property.Name] = level;
                }
            }
        }

        return new HorizonCharacterDetailResult(
            name,
            TryGetInt32(root, "nation"),
            GetString(root, "rank"),
            TryGetInt32(root, "mentor") == 1,
            TryGetInt64(root, "settings"),
            GetString(root, "jobString"),
            GetString(root, "avatar"),
            ResolvePortraitUrl(root),
            GetString(root, "seacom_type"),
            GetString(root, "seacom_message"),
            TryGetInt32(root, "online") == 1,
            jobs);
    }

    private static string? BuildPortraitUrlFromName(string? portraitName)
    {
        if (string.IsNullOrWhiteSpace(portraitName))
        {
            return null;
        }

        var normalizedPortraitName = portraitName.Trim();
        if (normalizedPortraitName.Contains('/') || normalizedPortraitName.Contains('\\'))
        {
            return null;
        }

        var hasOnlySafeCharacters = normalizedPortraitName.All(character =>
            char.IsLetterOrDigit(character)
            || character == '_'
            || character == '-'
            || character == '.');

        if (!hasOnlySafeCharacters)
        {
            return null;
        }

        if (normalizedPortraitName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return $"{HorizonPortraitBaseUrl}{normalizedPortraitName}";
        }

        return $"{HorizonPortraitBaseUrl}{normalizedPortraitName}.webp";
    }

    private static IEnumerable<JsonElement> GetResultArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            return results.EnumerateArray();
        }

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            return data.EnumerateArray();
        }

        if (root.TryGetProperty("chars", out var chars) && chars.ValueKind == JsonValueKind.Array)
        {
            return chars.EnumerateArray();
        }

        return [];
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return TryGetInt32(property, out var value) ? value : null;
    }

    private static long? TryGetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var value))
        {
            return value;
        }

        return long.TryParse(property.ToString(), out var parsedValue) ? parsedValue : null;
    }

    private static bool TryGetInt32(JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        return int.TryParse(element.ToString(), out value);
    }
}
