using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Dock.Core.Worktrees;

public sealed record ExternalOutputModel(int ExitCode, string Output)
{
    public bool Succeeded => ExitCode == 0;
}

public static class ExternalCommand
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);
    private static readonly UTF8Encoding Utf8 = new(false);

    public static ExternalOutputModel Run(string executable, IEnumerable<string> arguments, string? input = null, IReadOnlyDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8,
            StandardOutputEncoding = Utf8,
            StandardErrorEncoding = Utf8
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            info.Environment[name] = value;
        }

        try
        {
            using var process = Process.Start(info) ?? throw new WorktreeException($"Impossible de lancer {executable}.", WorktreeSteps.Database);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (input is not null)
            {
                process.StandardInput.Write(input);
            }

            process.StandardInput.Close();
            if (!process.WaitForExit(Timeout))
            {
                process.Kill(true);
                throw new WorktreeException($"{executable} ne répond plus après {Timeout.TotalMinutes:0} minutes : commande arrêtée.", WorktreeSteps.Database);
            }

            process.WaitForExit();
            var text = string.Join('\n', new[] { output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult() }.Where(part => part.Trim().Length > 0));
            return new ExternalOutputModel(process.ExitCode, text.Replace("\r\n", "\n").Trim());
        }
        catch (Win32Exception)
        {
            throw new WorktreeException($"{executable} est introuvable : installez-le ou ajoutez-le au PATH.", WorktreeSteps.Database);
        }
    }
}
