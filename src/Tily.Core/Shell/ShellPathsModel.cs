namespace Tily.Core.Shell;

public sealed record ShellPathsModel(IReadOnlyDictionary<string, string> Executables)
{
    public static readonly ShellPathsModel Empty = new(new Dictionary<string, string>());

    public string? ExecutableFor(string shellId) =>
        Executables.TryGetValue(shellId, out var executable) && !string.IsNullOrWhiteSpace(executable) ? executable : null;
}
