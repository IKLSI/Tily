namespace Tily.Core.Shell;

public sealed record ShellProfileModel(string Id, string Name, string Executable, string Arguments, bool Available);

public static class ShellCatalog
{
    public const string DefaultShellId = "zsh";
    public const string BashShellId = "bash";

    private const string ZshExecutable = "/bin/zsh";
    private const string BashExecutable = "/bin/bash";

    public static IReadOnlyList<ShellProfileModel> Profiles(ShellPathsModel? paths = null)
    {
        var overrides = paths ?? ShellPathsModel.Empty;
        var zsh = overrides.ExecutableFor(DefaultShellId) ?? ZshExecutable;
        var bash = overrides.ExecutableFor(BashShellId) ?? BashExecutable;
        return
        [
            new ShellProfileModel(DefaultShellId, "zsh", zsh, ShellIntegration.ZshArguments, File.Exists(zsh)),
            new ShellProfileModel(BashShellId, "bash", bash, ShellIntegration.BashArguments, File.Exists(bash))
        ];
    }

    public static ShellProfileModel Resolve(string shellId, ShellPathsModel? paths = null)
    {
        var profiles = Profiles(paths);
        var profile = profiles.FirstOrDefault(candidate => candidate.Id == shellId);
        if (profile is null)
        {
            throw new InvalidOperationException($"Shell inconnu : {shellId}");
        }

        if (!profile.Available)
        {
            throw new InvalidOperationException($"Le shell « {profile.Name} » est introuvable : {profile.Executable}. Chemin configurable dans {ShellPathsRepository.FileName}.");
        }

        return profile;
    }

    public static bool IsKnown(string? shellId) => shellId is DefaultShellId or BashShellId;

    public static bool ReportsCurrentDirectory(string shellId) => IsKnown(shellId);
}
