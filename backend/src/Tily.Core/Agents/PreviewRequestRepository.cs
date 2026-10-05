using System.Text.Json;
using Tily.Core.Files;
using Tily.Core.Session;

namespace Tily.Core.Agents;

public sealed record PreviewRequestModel(string PaneId, string Path);

public sealed record PreviewRequestFileModel(string? Pane, string? Path);

public sealed class PreviewRequestRepository
{
    private const string RequestPattern = "*.json";

    public PreviewRequestRepository(string dataDirectory)
    {
        Directory = System.IO.Path.Combine(dataDirectory, "previews");
    }

    public string Directory { get; }

    public void Clear()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return;
        }

        foreach (var file in System.IO.Directory.EnumerateFiles(Directory))
        {
            TryDelete(file);
        }
    }

    public IReadOnlyList<PreviewRequestModel> TakeAll()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return [];
        }

        var requests = new List<PreviewRequestModel>();
        foreach (var file in new DirectoryInfo(Directory).EnumerateFiles(RequestPattern).OrderBy(file => file.LastWriteTimeUtc).Select(file => file.FullName))
        {
            var request = Read(file);
            TryDelete(file);
            if (request is not null)
            {
                requests.Add(request);
            }
        }

        return requests;
    }

    private static PreviewRequestModel? Read(string file)
    {
        PreviewRequestFileModel? request;
        try
        {
            request = JsonSerializer.Deserialize<PreviewRequestFileModel>(File.ReadAllText(file), SessionRepository.JsonOptions);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request?.Pane) || request.Path is not { } path || !System.IO.Path.IsPathFullyQualified(path) || !File.Exists(path) || PreviewTypes.KindOf(path) != PreviewKind.Html)
        {
            return null;
        }

        return new PreviewRequestModel(request.Pane.Trim(), System.IO.Path.GetFullPath(path));
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
