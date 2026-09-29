using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Dock.Core.Worktrees;

public sealed record PortRandomizationModel(IReadOnlyList<PortChangeModel> Ports, IReadOnlyList<string> Files, IReadOnlyList<int> Unavailable);

public sealed partial class PortRandomizer
{
    private const int MinimumPort = 1024;
    private const int MaximumPort = 65535;
    private const int Attempts = 200;
    private const int SearchDepth = 12;
    private const int SuffixRange = 1000;

    private static readonly string[] Candidates =
    [
        @"client\vite.config.ts", @"client\vite.config.js", @"client\vite.config.mts",
        @"client\package.json",
        "docker-compose.yml", "docker-compose.yaml", "docker-compose.override.yml",
        ".env", ".env.local", ".env.development",
        @"client\.env", @"client\.env.local", @"client\.env.development", @"client\.env.production",
        @"client\scripts\routes-generator.ts", @"client\src\routes-generator.ts"
    ];

    private readonly Func<int, bool> _isFree;
    private readonly Random _random;

    public PortRandomizer(Func<int, bool>? isFree = null, Random? random = null)
    {
        _isFree = isFree ?? IsFree;
        _random = random ?? Random.Shared;
    }

    public PortRandomizationModel Randomize(string root)
    {
        var files = FilesOf(root);
        var contents = files.Select(file => (File: file, Content: WorktreeFiles.Read(file))).Where(entry => entry.Content is not null).ToList();
        var found = contents
            .SelectMany(entry => ContextualPort().Matches(entry.Content!).Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)))
            .Where(port => port is >= MinimumPort and <= MaximumPort)
            .Distinct()
            .Order()
            .ToList();
        var mapping = new Dictionary<int, int>();
        var unavailable = new List<int>();
        foreach (var port in found)
        {
            if (FreePortLike(port, found, mapping) is { } replacement)
            {
                mapping[port] = replacement;
            }
            else
            {
                unavailable.Add(port);
            }
        }

        var changed = new List<string>();
        foreach (var (file, content) in contents)
        {
            var updated = DelimitedPort().Replace(content!, match => mapping.TryGetValue(int.Parse(match.Value, CultureInfo.InvariantCulture), out var replacement) ? replacement.ToString(CultureInfo.InvariantCulture) : match.Value);
            if (updated != content)
            {
                WorktreeFiles.Write(file, updated);
                changed.Add(Path.GetRelativePath(root, file));
            }
        }

        return new PortRandomizationModel(mapping.Select(pair => new PortChangeModel(pair.Key, pair.Value)).ToList(), changed, unavailable);
    }

    public static string Describe(PortRandomizationModel result) =>
        result.Ports.Count == 0
            ? "Aucun port détecté dans les fichiers de configuration."
            : $"Ports : {string.Join(", ", result.Ports.Select(change => $"{change.Old} → {change.New}"))} ({result.Files.Count} fichier{(result.Files.Count > 1 ? "s" : string.Empty)} modifié{(result.Files.Count > 1 ? "s" : string.Empty)}).";

    private static IReadOnlyList<string> FilesOf(string root)
    {
        var explicitFiles = Candidates.Select(candidate => Path.Combine(root, candidate)).Where(File.Exists);
        var searched = WorktreeFiles.Find(root, IsSearchedFile, SearchDepth);
        return explicitFiles.Concat(searched).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsSearchedFile(string name) =>
        name.Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)
        || (name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

    private int? FreePortLike(int port, IReadOnlyCollection<int> existing, Dictionary<int, int> assigned)
    {
        var firstDigit = port.ToString(CultureInfo.InvariantCulture)[0];
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var candidate = int.Parse($"{firstDigit}{_random.Next(SuffixRange):D3}", CultureInfo.InvariantCulture);
            if (candidate < MinimumPort || existing.Contains(candidate) || assigned.ContainsValue(candidate))
            {
                continue;
            }

            if (_isFree(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    [GeneratedRegex("""(?:localhost:|://[^:/\s"]+:|"sslPort"\s*:\s*|"Port"\s*:\s*|\bport\s*[:=]\s*"?|applicationUrl[^"]*:|"PORT"\s*:\s*"?)(\d{4,5})""")]
    private static partial Regex ContextualPort();

    [GeneratedRegex(@"(?<![\d\.])\d{4,5}(?![\d\.])")]
    private static partial Regex DelimitedPort();
}
