using System.Security.Cryptography;
using System.Text;

namespace Tily.Core.Mcp;

public static class McpEndpoint
{
    public const string DataDirectoryVariable = "TILY_DATA_DIR";
    public const string PaneVariable = "TILY_PANE_ID";
    private const string PipePrefix = "tily-mcp-";
    private const int HashChars = 24;

    public static string DataDirectory(string? overridden, string localApplicationData) =>
        string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(localApplicationData, "Tily")
            : Path.GetFullPath(overridden);

    public static string DataDirectoryFromEnvironment() =>
        DataDirectory(Environment.GetEnvironmentVariable(DataDirectoryVariable), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string PipeName(string dataDirectory)
    {
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory)).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return PipePrefix + hash[..HashChars].ToLowerInvariant();
    }
}
