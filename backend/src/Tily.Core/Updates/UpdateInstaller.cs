using System.Diagnostics;

namespace Tily.Core.Updates;

public static class UpdateInstaller
{
    public const string ApplicationProcessVariable = "TILY_APP_PID";
    private const string BundleExtension = ".app";
    private const string ScriptName = "tily-update.sh";
    private const string Shell = "/bin/sh";

    public const string Script = """
        #!/bin/sh
        set -u
        exec </dev/null >/dev/null 2>&1
        DMG="$1"
        APP="$2"
        PID="$3"
        i=0
        while kill -0 "$PID" 2>/dev/null && [ "$i" -lt 120 ]; do
          sleep 0.5
          i=$((i + 1))
        done
        MOUNT=$(mktemp -d /tmp/tily-update.XXXXXX)
        if hdiutil attach -nobrowse -readonly -noautoopen -mountpoint "$MOUNT" "$DMG" >/dev/null 2>&1; then
          SOURCE=$(find "$MOUNT" -maxdepth 1 -name '*.app' -type d | head -n 1)
          if [ -n "$SOURCE" ]; then
            STAGED="$APP.update"
            rm -rf "$STAGED"
            if ditto "$SOURCE" "$STAGED"; then
              rm -rf "$APP" && mv "$STAGED" "$APP"
            fi
            rm -rf "$STAGED"
          fi
          hdiutil detach "$MOUNT" -quiet >/dev/null 2>&1 || hdiutil detach "$MOUNT" -force -quiet >/dev/null 2>&1
        fi
        rmdir "$MOUNT" 2>/dev/null
        rm -f "$DMG" "$0"
        open "$APP"
        """;

    public static string? BundleOf(string appDirectory)
    {
        var resources = Directory.GetParent(Path.TrimEndingDirectorySeparator(appDirectory));
        var bundle = resources?.Parent?.Parent;
        return bundle is not null && bundle.Name.EndsWith(BundleExtension, StringComparison.OrdinalIgnoreCase) && resources!.Name == "Resources" && resources.Parent!.Name == "Contents"
            ? bundle.FullName
            : null;
    }

    public static bool IsInstalled(string appDirectory) => BundleOf(appDirectory) is { } bundle && CanReplace(bundle);

    public static void Start(string installerPath, string appDirectory, int processId)
    {
        if (!File.Exists(installerPath))
        {
            throw new UpdateException($"Installeur introuvable : {installerPath}");
        }

        var bundle = BundleOf(appDirectory) ?? throw new UpdateException("Tily ne tourne pas depuis une application installée : mise à jour impossible.");
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
