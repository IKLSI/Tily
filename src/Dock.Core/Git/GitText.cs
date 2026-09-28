namespace Dock.Core.Git;

public static class GitText
{
    public static string Count(int count, string singular, string plural) => $"{count} {(count is 0 or 1 ? singular : plural)}";
}
