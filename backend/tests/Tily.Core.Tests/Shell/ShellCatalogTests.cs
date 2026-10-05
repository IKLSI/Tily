using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class ShellCatalogTests
{
    [Fact]
    public void Profiles_WhenPathOverriddenToExistingFile_ThenUsesItAndIsAvailable()
    {
        var executable = Path.GetTempFileName();
        var paths = new ShellPathsModel(new Dictionary<string, string> { [ShellCatalog.BashShellId] = executable });

        try
        {
            var profile = ShellCatalog.Profiles(paths).Single(candidate => candidate.Id == ShellCatalog.BashShellId);

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
        var paths = new ShellPathsModel(new Dictionary<string, string> { [ShellCatalog.BashShellId] = "/introuvable/bash" });

        var exception = Assert.Throws<InvalidOperationException>(() => ShellCatalog.Resolve(ShellCatalog.BashShellId, paths));

        Assert.Contains("/introuvable/bash", exception.Message);
        Assert.Contains(ShellPathsRepository.FileName, exception.Message);
    }

    [Fact]
    public void Resolve_WhenShellUnknown_ThenThrowsFrenchMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ShellCatalog.Resolve("powershell"));

        Assert.Equal("Shell inconnu : powershell", exception.Message);
    }

    [Fact]
    public void Profiles_WhenNoOverride_ThenDefaultShellIsSystemZsh()
    {
        var profile = ShellCatalog.Profiles().Single(candidate => candidate.Id == ShellCatalog.DefaultShellId);

        Assert.Equal("/bin/zsh", profile.Executable);
    }

    [Theory]
    [InlineData("zsh", true)]
    [InlineData("bash", true)]
    [InlineData("powershell", false)]
    [InlineData(null, false)]
    public void IsKnown_WhenShellId_ThenRecognizesOnlyMacShells(string? shellId, bool expected) =>
        Assert.Equal(expected, ShellCatalog.IsKnown(shellId));
}
