using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class ShellCatalogTests
{
    [Fact]
    public void Profiles_WhenPathOverriddenToExistingFile_ThenUsesItAndIsAvailable()
    {
        var executable = Path.GetTempFileName();
        var paths = new ShellPathsModel(new Dictionary<string, string> { ["gitbash"] = executable });

        try
        {
            var profile = ShellCatalog.Profiles(paths).Single(candidate => candidate.Id == "gitbash");

            Assert.Equal(executable, profile.Executable);
            Assert.True(profile.Available);
        }
        finally
        {
            File.Delete(executable);
        }
    }

    [Fact]
    public void Resolve_WhenPathOverriddenToMissingFile_ThenFailsWithConfigurationHint()
    {
        var paths = new ShellPathsModel(new Dictionary<string, string> { ["pwsh"] = @"C:\introuvable\pwsh.exe" });

        var exception = Assert.Throws<InvalidOperationException>(() => ShellCatalog.Resolve("pwsh", paths));

        Assert.Contains(@"C:\introuvable\pwsh.exe", exception.Message);
        Assert.Contains(ShellPathsRepository.FileName, exception.Message);
    }

    [Fact]
    public void Profiles_WhenNoOverride_ThenDefaultShellIsWindowsPowerShell()
    {
        var profile = ShellCatalog.Profiles().Single(candidate => candidate.Id == ShellCatalog.DefaultShellId);

        Assert.EndsWith(@"WindowsPowerShell\v1.0\powershell.exe", profile.Executable, StringComparison.OrdinalIgnoreCase);
    }
}
