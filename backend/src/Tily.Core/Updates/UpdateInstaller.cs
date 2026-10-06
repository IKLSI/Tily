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

    public const string Script = """
        #!/bin/sh
        set -u
        exec </dev/null >/dev/null 2>&1
        DMG="$1"
        APP="$2"
        PID="$3"
        BUNDLE_ID="$4"
        i=0
        while kill -0 "$PID" 2>/dev/null && [ "$i" -lt 120 ]; do
          sleep 0.5
          i=$((i + 1))
        done
        MOUNT=$(mktemp -d /tmp/tily-update.XXXXXX)
        if hdiutil attach -nobrowse -readonly -noautoopen -mountpoint "$MOUNT" "$DMG" >/dev/null 2>&1; then
          SOURCE=$(find "$MOUNT" -maxdepth 1 -name '*.app' -type d | head -n 1)
          if [ -n "$SOURCE" ] && [ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$SOURCE/Contents/Info.plist" 2>/dev/null)" = "$BUNDLE_ID" ]; then
            STAGED="$APP.update"
            PREVIOUS="$APP.old"
            rm -rf "$STAGED" "$PREVIOUS"
            if ditto "$SOURCE" "$STAGED" && mv "$APP" "$PREVIOUS"; then
              if mv "$STAGED" "$APP"; then
                rm -rf "$PREVIOUS"
              else
                rm -rf "$APP"
                mv "$PREVIOUS" "$APP"
              fi
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
        var script = Path.Combine(Path.GetDirectoryName(installerPath)!, ScriptName);
        File.WriteAllText(script, Script.ReplaceLineEndings("\n") + "\n");
        var start = new ProcessStartInfo(Shell)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { script, installerPath, bundle, ApplicationProcess(processId).ToString(), BundleIdentifier })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new UpdateException("Le script de mise à jour n’a pas pu être lancé.");
        process.StandardInput.Close();
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
