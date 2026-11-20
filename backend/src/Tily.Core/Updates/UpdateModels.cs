namespace Tily.Core.Updates;

public sealed record UpdateAssetModel(string Name, string Url, long Size, string Sha256);

public sealed record ReleaseNoteSectionModel(string? Title, IReadOnlyList<string> Notes);

public sealed record ReleaseHistoryModel(string Version, DateTimeOffset? PublishedAt, string PageUrl, IReadOnlyList<ReleaseNoteSectionModel> Sections);

public sealed record UpdateReleaseModel(string Version, string Name, IReadOnlyList<string> Notes, string PageUrl, DateTimeOffset? PublishedAt, UpdateAssetModel Installer)
{
    public bool IsNewerThan(string currentVersion) =>
        System.Version.TryParse(Version, out var available)
        && System.Version.TryParse(currentVersion, out var current)
        && available > current;
}

public sealed class UpdateException(string message) : Exception(message);
