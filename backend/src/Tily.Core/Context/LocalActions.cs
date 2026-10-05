using System.ComponentModel;
using System.Diagnostics;

namespace Tily.Core.Context;

public static class LocalActions
{
    public const string OpenExecutable = "/usr/bin/open";
    private const string RevealOption = "-R";

    private static readonly HashSet<string> LocalDocumentExtensions = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm", ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".txt", ".md" };

    public static void OpenInExplorer(string path)
    {
        RequireDirectory(path);
        Launch(Open(path), "Le Finder n’a pas pu être lancé");
    }

    public static void RevealInExplorer(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        Launch(Open(RevealOption, Path.GetFullPath(path)), "Le Finder n’a pas pu afficher l’élément");
    }

    private static ProcessStartInfo Open(params string[] arguments)
    {
        var start = new ProcessStartInfo(OpenExecutable) { UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return start;
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

        var executable = CommandLocator.Find(editorCommand) ?? throw new InvalidOperationException($"L’éditeur « {editorCommand} » est introuvable dans le PATH.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        Launch(start, $"L’éditeur « {editorCommand} » n’a pas pu être lancé");
    }

    public static void OpenLink(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            Launch(Open(RequireLocalDocument(uri)), "Fichier non ouvert");
            return;
        }

        Launch(Open(RequireWebLink(url).AbsoluteUri), "Lien non ouvert");
    }

    public static string RequireLocalDocument(Uri uri)
    {
        var path = uri.LocalPath;
        if (uri.IsUnc)
        {
            throw new InvalidOperationException($"Lien non ouvert : un fichier réseau ne s’ouvre pas depuis un lien file: ({path}).");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Fichier introuvable : {path}");
        }

        if (!LocalDocumentExtensions.Contains(Path.GetExtension(path)))
        {
            throw new InvalidOperationException($"Lien non ouvert : seuls les pages HTML, PDF, images et fichiers texte locaux s’ouvrent depuis un lien file: ({Path.GetFileName(path)}).");
        }

        return path;
    }

    private static void Launch(ProcessStartInfo start, string failure)
    {
        try
        {
            start.RedirectStandardInput = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            var process = Process.Start(start);
            if (process is not null)
            {
                process.StandardInput.Close();
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.Exited += (_, _) => process.Dispose();
                process.EnableRaisingEvents = true;
            }
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
