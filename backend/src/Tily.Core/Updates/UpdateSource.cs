namespace Tily.Core.Updates;

public static class UpdateSource
{
    public const string Repository = "IKLSI/Tily";
    public const string LatestReleaseUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
    public const string HistoryUrl = "https://api.github.com/repos/" + Repository + "/releases?per_page=100";
    public const string ReleasesPage = "https://github.com/" + Repository + "/releases";
}
