using System.Text.Json;

namespace Dock.Core.Updates;

public static class ReleaseParser
{
    public const string DownloadPrefix = "https://github.com/" + UpdateSource.Repository + "/releases/download/";
    private const string InstallerSuffix = "-setup.exe";
    private const string Sha256Prefix = "sha256:";

    public static UpdateReleaseModel Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var tag = Text(root, "tag_name");
            var version = tag.TrimStart('v', 'V');
            if (!System.Version.TryParse(version, out _))
            {
                throw new UpdateException($"Numéro de version illisible dans la release : « {tag} ».");
            }

            var name = Text(root, "name");
            DateTimeOffset? publishedAt = root.TryGetProperty("published_at", out var published) && published.ValueKind == JsonValueKind.String && published.TryGetDateTimeOffset(out var date) ? date : null;
            return new UpdateReleaseModel(
                version,
                name.Length > 0 ? name : $"Dock {version}",
                ReleaseNotes.Highlights(Text(root, "body")),
                Text(root, "html_url"),
                publishedAt,
                Installer(root, version));
        }
        catch (JsonException)
        {
            throw new UpdateException("Réponse de GitHub illisible.");
        }
        catch (InvalidOperationException)
        {
            throw new UpdateException("Réponse de GitHub inattendue.");
        }
    }

    private static UpdateAssetModel Installer(JsonElement root, string version)
    {
        var assets = root.TryGetProperty("assets", out var list) && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray().ToList() : [];
        var expected = $"Dock-{version}{InstallerSuffix}";
        var asset = assets.FirstOrDefault(candidate => string.Equals(Text(candidate, "name"), expected, StringComparison.OrdinalIgnoreCase));
        if (asset.ValueKind != JsonValueKind.Object)
        {
            throw new UpdateException($"La release {version} ne contient pas d’installeur {expected}.");
        }

        var url = Text(asset, "browser_download_url");
        if (!url.StartsWith(DownloadPrefix, StringComparison.Ordinal))
        {
            throw new UpdateException($"Adresse de téléchargement refusée : {url}");
        }

        var digest = Text(asset, "digest");
        if (!digest.StartsWith(Sha256Prefix, StringComparison.OrdinalIgnoreCase) || digest.Length != Sha256Prefix.Length + 64)
        {
            throw new UpdateException($"L’installeur {expected} n’a pas d’empreinte SHA-256 publiée : impossible de vérifier le téléchargement.");
        }

        var size = asset.TryGetProperty("size", out var bytes) && bytes.TryGetInt64(out var value) ? value : 0;
        return new UpdateAssetModel(expected, url, size, digest[Sha256Prefix.Length..].ToLowerInvariant());
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
}
