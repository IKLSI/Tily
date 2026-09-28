using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Dock.Core.Git;

public sealed record GitRunOptionsModel(string? Input = null, bool NeutralLocale = false, bool LiteralPaths = false, TimeSpan? Timeout = null);

public sealed record GitOutputModel(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;

    public string Details => GitRunner.Clean(string.Join('\n', new[] { Error, Output }.Where(text => text.Trim().Length > 0)));
}

public sealed partial class GitRunner
{
    private const string GitExecutable = "git.exe";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
    private static readonly UTF8Encoding Utf8 = new(false);
    private static readonly Lazy<string?> InstalledGit = new(FindGit);
    private static readonly string[] CommonArguments = ["-c", "core.quotepath=false", "-c", "color.ui=false", "--no-optional-locks"];

    private readonly IReadOnlyDictionary<string, string?> _environment;

    public GitRunner(IReadOnlyDictionary<string, string?>? environment = null) => _environment = environment ?? new Dictionary<string, string?>();

    public static bool IsInstalled => InstalledGit.Value is not null;

    public GitOutputModel Run(string directory, IEnumerable<string> arguments, GitRunOptionsModel? options = null)
    {
        var settings = options ?? new GitRunOptionsModel();
        using var process = new Process { StartInfo = StartInfo(directory, arguments, settings) };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        WriteInput(process, settings.Input);
        Wait(process, settings.Timeout ?? DefaultTimeout);
        return new GitOutputModel(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    public byte[] ReadBytes(string directory, params string[] arguments)
    {
        using var process = new Process { StartInfo = StartInfo(directory, arguments, new GitRunOptionsModel()) };
        process.Start();
        using var buffer = new MemoryStream();
        var copy = process.StandardOutput.BaseStream.CopyToAsync(buffer);
        var error = process.StandardError.ReadToEndAsync();
        process.StandardInput.Close();
        Wait(process, DefaultTimeout);
        copy.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new GitCommandException($"La commande « git {arguments.FirstOrDefault()} » a échoué.", Clean(error.GetAwaiter().GetResult()));
        }

        return buffer.ToArray();
    }

    public static string Clean(string text) => AnsiEscape().Replace(text, string.Empty).Replace("\r\n", "\n").Trim();

    private ProcessStartInfo StartInfo(string directory, IEnumerable<string> arguments, GitRunOptionsModel options)
    {
        var info = new ProcessStartInfo(InstalledGit.Value ?? throw new GitCommandException("Git est introuvable : installez Git pour Windows ou ajoutez git.exe au PATH.", string.Empty))
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8,
            StandardOutputEncoding = Utf8,
            StandardErrorEncoding = Utf8
        };
        foreach (var argument in CommonArguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (options.LiteralPaths)
        {
            info.ArgumentList.Add("--literal-pathspecs");
        }

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["GIT_EDITOR"] = "true";
        info.Environment["GIT_MERGE_AUTOEDIT"] = "no";
        if (options.NeutralLocale)
        {
            info.Environment["LC_ALL"] = "C";
        }

        foreach (var (name, value) in _environment)
        {
            info.Environment[name] = value;
        }

        return info;
    }

    private static void WriteInput(Process process, string? input)
    {
        if (input is not null)
        {
            process.StandardInput.Write(input);
        }

        process.StandardInput.Close();
    }

    private static void Wait(Process process, TimeSpan timeout)
    {
        if (process.WaitForExit(timeout))
        {
            process.WaitForExit();
            return;
        }

        process.Kill(true);
        throw new GitCommandException($"Git ne répond plus après {timeout.TotalMinutes:0} minutes : commande arrêtée.", string.Empty);
    }

    private static string? FindGit()
    {
        var directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidates = directories
            .Select(directory => directory.Trim('"'))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd"));
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
