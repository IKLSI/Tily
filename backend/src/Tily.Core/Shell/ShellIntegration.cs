namespace Tily.Core.Shell;

public sealed record ShellLaunchModel(string Executable, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Environment);

public static class ShellIntegration
{
    public const string ZshArguments = "-l";
    public const string BashArguments = "--init-file <intégration Tily> -i";
    private const string ZshFolder = "zsh";
    private const string BashFolder = "bash";
    private const string BashFile = "tily-integration.bash";
    private const string UserZdotdirVariable = "TILY_USER_ZDOTDIR";
    private const string ZdotdirVariable = "ZDOTDIR";

    private static readonly (string Name, string Content)[] ZshFiles =
    [
        (".zshenv", ZshIntegrationScripts.Environment),
        (".zprofile", ZshIntegrationScripts.Profile),
        (".zshrc", ZshIntegrationScripts.Startup),
        ("tily-integration.zsh", ZshIntegrationScripts.Integration)
    ];

    public static void Prepare(string directory)
    {
        var zsh = Path.Combine(directory, ZshFolder);
        foreach (var (name, content) in ZshFiles)
        {
            WriteIfChanged(Path.Combine(zsh, name), content);
        }

        WriteIfChanged(Path.Combine(directory, BashFolder, BashFile), BashIntegrationScript.Content);
    }

    public static ShellLaunchModel Launch(ShellProfileModel profile, string directory) => profile.Id switch
    {
        ShellCatalog.DefaultShellId => new ShellLaunchModel(profile.Executable, [ZshArguments], new Dictionary<string, string>
        {
            [ZdotdirVariable] = Path.Combine(directory, ZshFolder),
            [UserZdotdirVariable] = UserZdotdir()
        }),
        ShellCatalog.BashShellId => new ShellLaunchModel(profile.Executable, ["--init-file", Path.Combine(directory, BashFolder, BashFile), "-i"], new Dictionary<string, string>()),
        _ => throw new InvalidOperationException($"Shell inconnu : {profile.Id}")
    };

    private static string UserZdotdir()
    {
        var current = Environment.GetEnvironmentVariable(ZdotdirVariable);
        return string.IsNullOrWhiteSpace(current) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : current;
    }

    private static void WriteIfChanged(string path, string content)
    {
        var text = content.ReplaceLineEndings("\n") + "\n";
        if (File.Exists(path) && File.ReadAllText(path) == text)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
