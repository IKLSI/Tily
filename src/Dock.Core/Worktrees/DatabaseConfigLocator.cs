using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Dock.Core.Worktrees;

public static class DatabaseConfigLocator
{
    private const string DefaultConnection = "DefaultConnection";
    private const string DomainContext = "DomainContext";

    private static readonly (string Name, bool Json)[] Priority =
    [
        ("appsettings.Development.json", true),
        ("appsettings.Local.json", true),
        ("appsettings.json", true),
        ("Web.config", false),
        ("App.config", false)
    ];

    private static readonly JsonDocumentOptions JsonOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static DatabaseInfoModel? Locate(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        foreach (var (name, json) in Priority)
        {
            var files = WorktreeFiles.Find(root, candidate => candidate.Equals(name, StringComparison.OrdinalIgnoreCase)).OrderBy(file => file.Length);
            foreach (var file in files)
            {
                var connection = json ? FromJson(file) : FromXml(file);
                if (connection is { } found && DatabaseInfoModel.Parse(found.Value, file, found.Key) is { } info)
                {
                    return info;
                }
            }
        }

        return null;
    }

    private static KeyValuePair<string, string>? FromJson(string file)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file), JsonOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("ConnectionStrings", out var strings)
                || strings.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var entries = strings.EnumerateObject()
                .Where(property => property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { Length: > 0 })
                .Select(property => new KeyValuePair<string, string>(property.Name, property.Value.GetString()!))
                .ToList();
            return entries.Any(entry => entry.Key == DefaultConnection) ? entries.First(entry => entry.Key == DefaultConnection) : entries.Count > 0 ? entries[0] : null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static KeyValuePair<string, string>? FromXml(string file)
    {
        try
        {
            var entries = XDocument.Load(file)
                .Descendants("connectionStrings")
                .Elements("add")
                .Select(element => new KeyValuePair<string, string>((string?)element.Attribute("name") ?? string.Empty, (string?)element.Attribute("connectionString") ?? string.Empty))
                .ToList();
            var preferred = entries.FirstOrDefault(entry => entry.Key == DefaultConnection);
            if (preferred.Key is null)
            {
                preferred = entries.FirstOrDefault(entry => entry.Key == DomainContext);
            }

            if (preferred.Key is null && entries.Count > 0)
            {
                preferred = entries[0];
            }

            return preferred.Key is null || preferred.Value.Length == 0 ? null : preferred;
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
