using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Tily.Core.Shell;
using Tily.Core.Terminal;
using Xunit;

namespace Tily.Core.Tests.Terminal;

[Collection(TerminalCollection.Name)]
public sealed class TerminalManagerTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);
    private const string Escape = "\u001b";

    private readonly string _integration = Path.Combine(Path.GetTempPath(), $"tily-integration-{Guid.NewGuid():N}");
    private readonly string _home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Fact]
    public async Task Start_WhenZsh_ThenReportsCurrentDirectoryAndInjectsPaneVariable()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, output, reported) = await StartAsync(manager, "pane-test", ShellCatalog.DefaultShellId);

        session.Write(Encoding.UTF8.GetBytes("print -r -- \"TILYVAR|$TILY_PANE_ID|FIN\"\r"));
        await WaitForAsync(() => Text(output).Contains("TILYVAR|pane-test|FIN"));

        Assert.Equal(Path.TrimEndingDirectorySeparator(_home), Path.TrimEndingDirectorySeparator(reported));
    }

    [Fact]
    public async Task Resize_WhenZshRunning_ThenShellSeesStartAndNewSize()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, output, _) = await StartAsync(manager, "pane-size", ShellCatalog.DefaultShellId);

        session.Write(Encoding.UTF8.GetBytes("print -r -- \"SIZE|$(stty size)|FIN\"\r"));
        await WaitForAsync(() => Text(output).Contains("SIZE|30 100|FIN"));

        session.Resize(120, 40);
        session.Write(Encoding.UTF8.GetBytes("print -r -- \"SIZE|$(stty size)|FIN\"\r"));
        await WaitForAsync(() => Text(output).Contains("SIZE|40 120|FIN"));
    }

    [Fact]
    public async Task Write_WhenZshRunsCommand_ThenAnnouncesExecutionEndAndPrompt()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, output, _) = await StartAsync(manager, "pane-osc", ShellCatalog.DefaultShellId);
        var command = Convert.ToBase64String(Encoding.UTF8.GetBytes("false"));

        session.Write(Encoding.UTF8.GetBytes("false\r"));

        await WaitForAsync(() => Text(output).Contains($"{Escape}]6973;exec;{command}{Escape}\\"));
        await WaitForAsync(() => Text(output).Contains($";0;{command}{Escape}\\") && Text(output).Contains($"{Escape}]6973;done;"));
        Assert.Contains($"{Escape}]6973;prompt;", Text(output));
    }

    [Fact]
    public async Task Start_WhenBash_ThenReportsCurrentDirectoryAndCommandEnd()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, output, reported) = await StartAsync(manager, "pane-bash", ShellCatalog.BashShellId);
        var command = Convert.ToBase64String(Encoding.UTF8.GetBytes("true"));

        session.Write(Encoding.UTF8.GetBytes("true\r"));

        await WaitForAsync(() => Text(output).Contains($";1;{command}{Escape}\\"));
        Assert.Equal(Path.TrimEndingDirectorySeparator(_home), Path.TrimEndingDirectorySeparator(reported));
    }

    [Fact]
    public async Task Stop_WhenChildProcessesRunning_ThenNoneSurvive()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, _, _) = await StartAsync(manager, "pane-job", ShellCatalog.DefaultShellId);
        session.Write(Encoding.UTF8.GetBytes("sleep 300 & sleep 301 &\r"));
        await WaitForAsync(() => session.ProcessIds().Count >= 3);
        var processes = session.ProcessIds().Select(OpenProcess).OfType<Process>().ToList();

        manager.Stop("pane-job");

        try
        {
            Assert.All(processes, process => Assert.True(process.WaitForExit(StopTimeout), $"Processus {process.Id} encore vivant après l’arrêt du pane."));
        }
        finally
        {
            processes.ForEach(process => process.Dispose());
        }
    }

    [Fact]
    public async Task Activity_WhenProgramRunning_ThenListsItWithoutTheShell()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, _, _) = await StartAsync(manager, "pane-activity", ShellCatalog.DefaultShellId);

        session.Write(Encoding.UTF8.GetBytes("sleep 300\r"));
        await WaitForAsync(() => RunsSleep(manager, "pane-activity"));
        var activity = manager.Activity(["pane-activity"]).Single();

        Assert.Equal("pane-activity", activity.PaneId);
        Assert.DoesNotContain("zsh", activity.Processes);
    }

    [Fact]
    public async Task Write_WhenCtrlC_ThenInterruptsForegroundProgram()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var (session, _, _) = await StartAsync(manager, "pane-ctrl-c", ShellCatalog.DefaultShellId);
        session.Write(Encoding.UTF8.GetBytes("sleep 300\r"));
        await WaitForAsync(() => RunsSleep(manager, "pane-ctrl-c"));

        session.Write(Encoding.UTF8.GetBytes("\u0003"));

        Assert.True(await EventuallyAsync(() => !RunsSleep(manager, "pane-ctrl-c")), "Ctrl + C n’a pas interrompu le programme du pane.");
    }

    [Fact]
    public async Task Write_WhenShellExits_ThenReportsExitCode()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);
        var exited = new TaskCompletionSource<uint>();
        manager.Exited += (_, code) => exited.TrySetResult(code);
        var (session, _, _) = await StartAsync(manager, "pane-exit", ShellCatalog.DefaultShellId);

        session.Write(Encoding.UTF8.GetBytes("exit 3\r"));

        Assert.Equal(3u, await exited.Task.WaitAsync(Timeout));
    }

    [Fact]
    public void Activity_WhenPaneUnknown_ThenReportsNothing()
    {
        using var manager = new TerminalManager(integrationDirectory: _integration);

        var activity = manager.Activity(["pane-inconnu"]);

        Assert.Empty(activity);
    }

    private async Task<(TerminalSession Session, StringBuilder Output, string Directory)> StartAsync(TerminalManager manager, string paneId, string shellId)
    {
        var directory = new TaskCompletionSource<string>();
        var output = new StringBuilder();
        manager.CurrentDirectoryChanged += (pane, path) =>
        {
            if (pane == paneId)
            {
                directory.TrySetResult(path);
            }
        };
        manager.OutputReceived += (pane, data) =>
        {
            if (pane == paneId)
            {
                lock (output)
                {
                    output.Append(Encoding.UTF8.GetString(data.Span));
                }
            }
        };
        var session = manager.Start(paneId, shellId, _home, 100, 30);
        return (session, output, await directory.Task.WaitAsync(Timeout));
    }

    private static string Text(StringBuilder output)
    {
        lock (output)
        {
            return output.ToString();
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed > Timeout)
            {
                throw new TimeoutException("Condition non remplie dans le délai imparti.");
            }

            await Task.Delay(100);
        }
    }

    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        try
        {
            await WaitForAsync(condition);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static bool RunsSleep(TerminalManager manager, string paneId) =>
        manager.Activity([paneId]).Any(activity => activity.Processes.Contains("sleep"));

    private static Process? OpenProcess(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_integration))
        {
            Directory.Delete(_integration, true);
        }
    }
}
