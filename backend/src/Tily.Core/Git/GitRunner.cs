using System.Text;
using System.Text.RegularExpressions;
using Tily.Core.Processes;

namespace Tily.Core.Git;

public sealed record GitRunOptionsModel(string? Input = null, bool NeutralLocale = false, bool LiteralPaths = false, TimeSpan? Timeout = null);

public sealed record GitOutputModel(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;

    public string Details => GitRunner.Clean(string.Join('\n', new[] { Error, Output }.Where(text => text.Trim().Length > 0)));
}

public sealed partial class GitRunner
{
    private const string GitExecutable = "git";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
    private static readonly UTF8Encoding Utf8 = new(false);
    private static readonly Lazy<string?> InstalledGit = new(FindGit);
    private static readonly string[] CommonArguments = ["-c", "core.quotepath=false", "-c", "color.ui=false", "--no-optional-locks"];

    private readonly IReadOnlyDictionary<string, string?> _environment;

    public GitRunner(IReadOnlyDictionary<string, string?>? environment = null) => _environment = environment ?? new Dictionary<string, string?>();

    public static bool IsInstalled => InstalledGit.Value is not null;

    public GitOutputModel Run(string directory, IEnumerable<string> arguments, GitRunOptionsModel? options = null)
    {
        var result = Execute(Request(directory, arguments, options ?? new GitRunOptionsModel()));
        return new GitOutputModel(result.ExitCode, result.Output, result.Error);
    }

    public byte[] ReadBytes(string directory, params string[] arguments)
    {
        using var buffer = new MemoryStream();
        var result = Execute(Request(directory, arguments, new GitRunOptionsModel()) with { OutputDestination = buffer });
        if (result.ExitCode != 0)
        {
            throw new GitCommandException($"La commande « git {arguments.FirstOrDefault()} » a échoué.", Clean(result.Error));
        }

        return buffer.ToArray();
    }

    public static string Clean(string text) => AnsiEscape().Replace(text, string.Empty).Replace("\r\n", "\n").Trim();

    private ProcessRequestModel Request(string directory, IEnumerable<string> arguments, GitRunOptionsModel options)
    {
        if (!Directory.Exists(directory))
        {
            throw new GitCommandException($"Dossier introuvable : {directory}", string.Empty);
        }

        var executable = InstalledGit.Value ?? throw new GitCommandException("Git est introuvable : installez Git (Xcode Command Line Tools ou Homebrew) ou ajoutez git au PATH.", string.Empty);
        var allArguments = new List<string>(CommonArguments);
        if (options.LiteralPaths)
        {
            allArguments.Add("--literal-pathspecs");
        }

        allArguments.AddRange(arguments);
        var environment = new Dictionary<string, string?>
        {
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["GIT_EDITOR"] = "true",
            ["GIT_MERGE_AUTOEDIT"] = "no"
        };
        if (options.NeutralLocale)
        {
            environment["LC_ALL"] = "C";
        }

        foreach (var (name, value) in _environment)
        {
            environment[name] = value;
        }

        return new ProcessRequestModel(executable, allArguments, options.Timeout ?? DefaultTimeout, directory, environment, options.Input, Encoding: Utf8);
    }

    private static ProcessOutputModel Execute(ProcessRequestModel request)
    {
        var result = ProcessRunner.Run(request);
        if (result.TimedOut)
        {
            throw new GitCommandException($"Git ne répond plus après {request.Timeout.TotalMinutes:0} minutes : commande arrêtée.", string.Empty);
        }

        return result;
    }

    private static string? FindGit()
    {
        var directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidates = directories
            .Select(directory => directory.Trim('"'))
            .Concat(Tily.Core.Context.CommandLocator.FallbackDirectories);
        foreach (var directory in candidates)
        {
            try
            {
                var candidate = Path.Combine(directory, GitExecutable);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
            }
        }

        return null;
    }

    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex AnsiEscape();
}
