using System.ComponentModel;
using System.Diagnostics;
using Dock.Core.Native;

namespace Dock.Core.Context;

public static class LocalActions
{
    public const string ExplorerExecutable = "explorer.exe";

    public static void OpenInExplorer(string path)
    {
        RequireDirectory(path);
        Launch(new ProcessStartInfo(ExplorerExecutable) { ArgumentList = { path }, UseShellExecute = false }, "L’Explorateur Windows n’a pas pu être lancé");
    }

    public static void RevealInExplorer(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        var item = ShellFileApi.ILCreateFromPath(Path.GetFullPath(path));
        if (item == 0)
        {
            throw new InvalidOperationException($"Impossible d’afficher l’élément dans l’explorateur : {path}");
        }

        try
        {
            var code = ShellFileApi.SHOpenFolderAndSelectItems(item, 0, null, 0);
            if (code != 0)
            {
                throw new InvalidOperationException($"L’explorateur n’a pas pu afficher l’élément (code 0x{code:X8}) : {path}");
            }
        }
        finally
        {
            ShellFileApi.ILFree(item);
        }
    }

    public static void OpenInEditor(string path, string editorCommand)
    {
        RequireDirectory(path);
        LaunchEditor(path, editorCommand);
    }

    public static void OpenFileInEditor(string path, string editorCommand, int line = 0, int column = 0)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Le fichier n’existe plus : {path}");
        }

        LaunchEditor(EditorLocation.Arguments(editorCommand, path, line, column), editorCommand);
    }

    private static void LaunchEditor(string path, string editorCommand) => LaunchEditor([path], editorCommand);

    private static void LaunchEditor(IReadOnlyList<string> arguments, string editorCommand)
    {
        if (string.IsNullOrWhiteSpace(editorCommand))
        {
            throw new InvalidOperationException("Aucun éditeur configuré.");
        }

        Launch(new ProcessStartInfo(editorCommand, EditorLocation.CommandLine(arguments)) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden }, $"L’éditeur « {editorCommand} » n’a pas pu être lancé");
    }

    public static void OpenLink(string url) =>
        Launch(new ProcessStartInfo(RequireWebLink(url).AbsoluteUri) { UseShellExecute = true }, "Lien non ouvert");

    private static void Launch(ProcessStartInfo start, string failure)
    {
        try
        {
            Process.Start(start);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"{failure} : {new Win32Exception(exception.NativeErrorCode).Message}");
        }
        catch (FileNotFoundException exception)
        {
            throw new InvalidOperationException($"{failure} : {exception.Message}");
        }
    }

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
