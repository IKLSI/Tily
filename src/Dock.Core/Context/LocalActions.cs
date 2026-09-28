using System.Diagnostics;

namespace Dock.Core.Context;

public static class LocalActions
{
    public const string ExplorerExecutable = "explorer.exe";
    public const string SelectSwitch = "/select,";

    public static void OpenInExplorer(string path)
    {
        RequireDirectory(path);
        Process.Start(new ProcessStartInfo(ExplorerExecutable) { ArgumentList = { path }, UseShellExecute = false });
    }

    public static void RevealInExplorer(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        var startInfo = new ProcessStartInfo(ExplorerExecutable) { UseShellExecute = false };
        foreach (var argument in RevealArguments(path))
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process.Start(startInfo);
    }

    public static IReadOnlyList<string> RevealArguments(string path) => [SelectSwitch, Path.GetFullPath(path)];

    public static void OpenInEditor(string path, string editorCommand)
    {
        RequireDirectory(path);
        LaunchEditor(path, editorCommand);
    }

    public static void OpenFileInEditor(string path, string editorCommand)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Le fichier n’existe plus : {path}");
        }

        LaunchEditor(path, editorCommand);
    }

    private static void LaunchEditor(string path, string editorCommand)
    {
        if (string.IsNullOrWhiteSpace(editorCommand))
        {
            throw new InvalidOperationException("Aucun éditeur configuré.");
        }

        try
        {
            Process.Start(new ProcessStartInfo(editorCommand) { ArgumentList = { path }, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException($"L’éditeur « {editorCommand} » n’a pas pu être lancé : {exception.Message}");
        }
    }

    public static void OpenLink(string url) =>
        Process.Start(new ProcessStartInfo(RequireWebLink(url).AbsoluteUri) { UseShellExecute = true });

    public static Uri RequireWebLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Lien non ouvert, adresse invalide : {url}");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Lien non ouvert : seuls les liens http et https sont autorisés ({uri.Scheme}:).");
        }

        return uri;
    }

    private static void RequireDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new InvalidOperationException($"Le dossier n’existe plus : {path}");
        }
    }
}
