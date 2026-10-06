using System.ComponentModel;
using System.Text;
using Tily.Core.Processes;

namespace Tily.Core.Worktrees;

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
        var request = new ProcessRequestModel(
            Tily.Core.Context.CommandLocator.Find(executable) ?? executable,
            arguments,
            Timeout,
            Environment: environment?.ToDictionary(pair => pair.Key, pair => (string?)pair.Value),
            Input: input,
            Encoding: Utf8);
        try
        {
            var result = ProcessRunner.Run(request);
            if (result.TimedOut)
            {
                throw new WorktreeException($"{executable} ne répond plus après {Timeout.TotalMinutes:0} minutes : commande arrêtée.", WorktreeSteps.Database);
            }

            var text = string.Join('\n', new[] { result.Output, result.Error }.Where(part => part.Trim().Length > 0));
            return new ExternalOutputModel(result.ExitCode, text.Replace("\r\n", "\n").Trim());
        }
        catch (Win32Exception)
        {
            throw new WorktreeException($"{executable} est introuvable : installez-le ou ajoutez-le au PATH.", WorktreeSteps.Database);
        }
    }
}
