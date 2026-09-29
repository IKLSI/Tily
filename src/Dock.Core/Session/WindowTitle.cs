namespace Dock.Core.Session;

public static class WindowTitle
{
    public const string ApplicationName = "Dock";
    public const int MaxContextLength = 160;
    private const string Separator = " - ";
    private const string Ellipsis = "…";

    public static string For(string? context)
    {
        var cleaned = new string((context ?? string.Empty).Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            return ApplicationName;
        }

        var shortened = cleaned.Length > MaxContextLength ? cleaned[..MaxContextLength].TrimEnd() + Ellipsis : cleaned;
        return shortened + Separator + ApplicationName;
    }
}
