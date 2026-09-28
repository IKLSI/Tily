namespace Dock.Core.Shell;

public sealed record ShellProfileModel(string Id, string Name, string Executable, string Arguments, bool Available);

public static class ShellCatalog
{
    public const string DefaultShellId = "powershell";

    private static readonly string System32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
    private static readonly string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    public static IReadOnlyList<ShellProfileModel> Profiles(ShellPathsModel? paths = null)
    {
        var overrides = paths ?? ShellPathsModel.Empty;
        var wrapper = PowerShellIntegration.EncodedPromptWrapper();
        var powershell = overrides.ExecutableFor(DefaultShellId) ?? Path.Combine(System32, "WindowsPowerShell", "v1.0", "powershell.exe");
        var pwsh = overrides.ExecutableFor("pwsh") ?? Path.Combine(ProgramFiles, "PowerShell", "7", "pwsh.exe");
        var cmd = overrides.ExecutableFor("cmd") ?? Path.Combine(System32, "cmd.exe");
        var gitBash = overrides.ExecutableFor("gitbash") ?? Path.Combine(ProgramFiles, "Git", "bin", "bash.exe");
        return new[]
        {
            new ShellProfileModel(DefaultShellId, "Windows PowerShell 5.1", powershell, $"-NoLogo -NoExit -EncodedCommand {wrapper}", File.Exists(powershell)),
            new ShellProfileModel("pwsh", "PowerShell 7", pwsh, $"-NoLogo -NoExit -EncodedCommand {wrapper}", File.Exists(pwsh)),
            new ShellProfileModel("cmd", "Invite de commandes", cmd, string.Empty, File.Exists(cmd)),
            new ShellProfileModel("gitbash", "Git Bash", gitBash, "--login -i", File.Exists(gitBash))
        };
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

    public static string CommandLine(ShellProfileModel profile) =>
        string.IsNullOrEmpty(profile.Arguments) ? $"\"{profile.Executable}\"" : $"\"{profile.Executable}\" {profile.Arguments}";
}
