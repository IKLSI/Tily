using Tily.Core.Native;

namespace Tily.Core.Files;

public static class RecycleBin
{
    public static void Send(string path, nint ownerWindow)
    {
        FileExplorer.RequireFullPath(path);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        var operation = new ShellFileApi.FileOperation
        {
            WindowHandle = ownerWindow,
            Function = ShellFileApi.OperationDelete,
            From = $"{path}\0",
            Flags = ShellFileApi.FlagAllowUndo | ShellFileApi.FlagNoConfirmation | ShellFileApi.FlagSilent | ShellFileApi.FlagNoErrorUi | ShellFileApi.FlagWantNukeWarning
        };
        var code = ShellFileApi.SHFileOperation(ref operation);
        if (operation.AnyOperationsAborted)
        {
            throw new InvalidOperationException($"Suppression annulée : {path}");
        }

        if (code != 0)
        {
            throw new InvalidOperationException($"Impossible de placer « {Path.GetFileName(path)} » dans la corbeille (code 0x{code:X}). Vérifiez qu’aucun programme ne l’utilise.");
        }
    }
}
