using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class ShellPathsRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFile_ThenReturnsEmptyAndWritesTemplate()
    {
        var repository = new ShellPathsRepository(_directory);

        var paths = repository.Load();

        Assert.Empty(paths.Executables);
        Assert.Contains("pwsh", File.ReadAllText(repository.FilePath));
    }

    [Fact]
    public void Load_WhenFileConfigured_ThenReturnsNonEmptyEntries()
    {
        var repository = new ShellPathsRepository(_directory);
        File.WriteAllText(repository.FilePath, """{ "cmd": "D:\\outils\\cmd.exe", "pwsh": "" }""");

        var paths = repository.Load();

        Assert.Equal(@"D:\outils\cmd.exe", paths.ExecutableFor("cmd"));
        Assert.Null(paths.ExecutableFor("pwsh"));
    }

    [Fact]
    public void Load_WhenInvalidJson_ThenReturnsEmpty()
    {
        var repository = new ShellPathsRepository(_directory);
        File.WriteAllText(repository.FilePath, "{ pas du json");

        var paths = repository.Load();

        Assert.Empty(paths.Executables);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
