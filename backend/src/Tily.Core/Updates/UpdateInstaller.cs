using System.Diagnostics;
using System.Security.Cryptography;

namespace Tily.Core.Updates;

public static class UpdateInstaller
{
    public const string ApplicationProcessVariable = "TILY_APP_PID";
    public const string BundleIdentifier = "com.iklsi.tily";
    private const string BundleExtension = ".app";
    private const string ScriptName = "tily-update.sh";
    private const string Shell = "/bin/sh";
    private const string StagedSuffix = ".update";
    private const string Hdiutil = "/usr/bin/hdiutil";
    private const string Ditto = "/usr/bin/ditto";
    private const string PlistBuddy = "/usr/libexec/PlistBuddy";
    private static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(5);

    public const string Script = """
        #!/bin/sh
        set -u
        DMG="$1"
        exec </dev/null >"$(dirname "$DMG")/../update.log" 2>&1
        set -x
        APP="$2"
        PID="$3"
        STAGED="$APP.update"
        PREVIOUS="$APP.old"
        i=0
        while kill -0 "$PID" 2>/dev/null && [ "$i" -lt 120 ]; do
          sleep 0.5
          i=$((i + 1))
        done
        rm -rf "$PREVIOUS"
        if mv "$APP" "$PREVIOUS"; then
          if mv "$STAGED" "$APP"; then
            rm -rf "$PREVIOUS"
          else
            rm -rf "$APP"
            mv "$PREVIOUS" "$APP"
          fi
        fi
        rm -rf "$STAGED"
        rm -f "$DMG" "$0"
        sleep 1
        open -n "$APP"
        """;

    public static string? BundleOf(string appDirectory)
    {
        var resources = Directory.GetParent(Path.TrimEndingDirectorySeparator(appDirectory));
        var bundle = resources?.Parent?.Parent;
        return bundle is not null && bundle.Name.EndsWith(BundleExtension, StringComparison.OrdinalIgnoreCase) && resources!.Name == "Resources" && resources.Parent!.Name == "Contents"
            ? bundle.FullName
            : null;
    }

    public static string StagedPath(string bundle) => bundle + StagedSuffix;

    public static void CleanStaged(string appDirectory)
    {
        if (BundleOf(appDirectory) is not { } bundle)
        {
            return;
        }

        try
        {
            Directory.Delete(StagedPath(bundle), true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    public static void Stage(string installerPath, string appDirectory)
    {
        var bundle = BundleOf(appDirectory) ?? throw new UpdateException("Tily ne tourne pas depuis une application installée : mise à jour impossible.");
        var staged = StagedPath(bundle);
        var mount = Directory.CreateTempSubdirectory("tily-update.").FullName;
        var attached = false;
        try
        {
            Run(Hdiutil, "Impossible de monter l’installeur", "attach", "-nobrowse", "-readonly", "-noverify", "-noautoopen", "-mountpoint", mount, installerPath);
            attached = true;
            var source = Directory.EnumerateDirectories(mount, "*" + BundleExtension).FirstOrDefault() ?? throw new UpdateException("L’installeur ne contient aucune application.");
            var identifier = Run(PlistBuddy, "Identifiant de l’application illisible", "-c", "Print :CFBundleIdentifier", Path.Combine(source, "Contents", "Info.plist")).Trim();
            if (identifier != BundleIdentifier)
            {
                throw new UpdateException("L’installeur ne contient pas Tily : rien n’a été installé.");
            }

            CleanStaged(appDirectory);
            Run(Ditto, "Copie de la nouvelle version impossible", source, staged);
        }
        catch
        {
            CleanStaged(appDirectory);
            throw;
        }
        finally
        {
            if (attached)
            {
                Detach(mount);
            }

            try
            {
                Directory.Delete(mount);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    public static bool IsInstalled(string appDirectory) => BundleOf(appDirectory) is { } bundle && CanReplace(bundle);

    public static void Start(string installerPath, string expectedSha256, string appDirectory, int processId)
    {
        if (!File.Exists(installerPath))
        {
            throw new UpdateException($"Installeur introuvable : {installerPath}");
        }

        if (!string.Equals(Sha256Of(installerPath), expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateException("L’installeur ne correspond plus à l’empreinte SHA-256 publiée : rien n’a été installé.");
        }

        var bundle = BundleOf(appDirectory) ?? throw new UpdateException("Tily ne tourne pas depuis une application installée : mise à jour impossible.");
        if (!Directory.Exists(StagedPath(bundle)))
        {
            throw new UpdateException("La nouvelle version n’a pas été préparée : rien n’a été installé.");
        }

        var script = Path.Combine(Path.GetDirectoryName(installerPath)!, ScriptName);
        File.WriteAllText(script, Script.ReplaceLineEndings("\n") + "\n");
        var start = new ProcessStartInfo(Shell)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { script, installerPath, bundle, ApplicationProcess(processId).ToString() })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new UpdateException("Le script de mise à jour n’a pas pu être lancé.");
        process.StandardInput.Close();
    }

    private static string Run(string program, string failure, params string[] arguments)
    {
        var start = new ProcessStartInfo(program) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new UpdateException($"{failure}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(StepTimeout))
        {
            process.Kill(true);
            throw new UpdateException($"{failure} : délai dépassé.");
        }

        Task.WaitAll(output, error);
        return process.ExitCode == 0 ? output.Result : throw new UpdateException($"{failure} : {error.Result.Trim()}");
    }

    private static void Detach(string mount)
    {
        try
        {
            Run(Hdiutil, "Démontage impossible", "detach", mount, "-quiet");
        }
        catch (UpdateException)
        {
            try
            {
                Run(Hdiutil, "Démontage impossible", "detach", mount, "-force", "-quiet");
            }
            catch (UpdateException)
            {
            }
        }
    }

    private static string Sha256Of(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    private static int ApplicationProcess(int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(ApplicationProcessVariable), out var processId) && processId > 0 ? processId : fallback;

    private static bool CanReplace(string bundle)
    {
        var parent = Path.GetDirectoryName(bundle);
        if (parent is null)
        {
            return false;
        }

        var probe = Path.Combine(parent, $".tily-update-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
