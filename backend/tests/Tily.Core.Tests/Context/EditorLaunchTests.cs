using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

public sealed class EditorLaunchTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-editeur-" + Guid.NewGuid().ToString("N"));

    public EditorLaunchTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void OpenFileInEditor_WhenPathHasShellCharacters_ThenEditorReceivesWholePath()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root, "R&md;injecte $(x) 'q'")).FullName;
        var file = Path.Combine(folder, "a.ts");
        File.WriteAllText(file, "x");
        var editor = Path.Combine(_root, "outils", "code");
        Directory.CreateDirectory(Path.GetDirectoryName(editor)!);
        File.WriteAllText(editor, "#!/bin/sh\nprintf '%s\n' \"$@\" > \"$(dirname \"$0\")/arguments.txt\"\n");
        File.SetUnixFileMode(editor, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        LocalActions.OpenFileInEditor(file, editor, 3, 1);
        var received = WaitForText(Path.Combine(_root, "outils", "arguments.txt"));

        Assert.Equal($"-g\n{file}:3:1", received);
    }

    [Fact]
    public void OpenFileInEditor_WhenEditorMissing_ThenExplainsInFrench()
    {
        var file = Path.Combine(_root, "a.ts");
        File.WriteAllText(file, "x");
        var editor = Path.Combine(_root, "absent", "editeur-introuvable");

        var exception = Assert.Throws<InvalidOperationException>(() => LocalActions.OpenFileInEditor(file, editor));

        Assert.Equal($"L’éditeur « {editor} » est introuvable dans le PATH.", exception.Message);
    }

    private static string WaitForText(string path)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var text = File.ReadAllText(path).Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }
            catch (IOException)
            {
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException("L'éditeur de test n'a rien reçu.");
    }

    public void Dispose()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(_root, true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
        }
    }
}
