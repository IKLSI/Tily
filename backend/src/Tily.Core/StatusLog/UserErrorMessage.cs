namespace Tily.Core.StatusLog;

public static class UserErrorMessage
{
    public static string Of(Exception exception) => exception switch
    {
        FileNotFoundException missing => $"Fichier introuvable : {missing.FileName ?? Detail(missing)}",
        DirectoryNotFoundException missing => $"Dossier introuvable ({Detail(missing)})",
        PathTooLongException tooLong => $"Chemin trop long ({Detail(tooLong)})",
        UnauthorizedAccessException denied => $"Accès refusé ({Detail(denied)})",
        IOException failure => $"Erreur de lecture ou d’écriture ({Detail(failure)})",
        _ => exception.Message
    };

    private static string Detail(Exception exception) => exception.Message.TrimEnd('.');
}
