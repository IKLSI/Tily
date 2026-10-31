using Tily.Core.Updates;
using Xunit;

namespace Tily.Core.Tests.Updates;

public sealed class UpdateInstallerTests
{
    [Fact]
    public void BundleOf_WhenBackendInsideApplication_ThenReturnsApplication()
    {
        var bundle = UpdateInstaller.BundleOf("/Applications/Tily.app/Contents/Resources/backend/");

        Assert.Equal("/Applications/Tily.app", bundle);
    }

    [Theory]
    [InlineData("/Users/moi/tily/backend/src/Tily.Host/bin/Debug/net10.0")]
    [InlineData("/Applications/Tily/Contents/Resources/backend")]
    [InlineData("/Applications/Tily.app/Contents/MacOS/backend")]
    public void BundleOf_WhenNotInsideApplication_ThenReturnsNull(string directory) =>
        Assert.Null(UpdateInstaller.BundleOf(directory));

    [Fact]
    public void IsInstalled_WhenRunFromBuildOutput_ThenFalse() =>
        Assert.False(UpdateInstaller.IsInstalled(AppContext.BaseDirectory));

    [Fact]
    public void Start_WhenInstallerMissing_ThenFailsInFrench()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"Tily-{Guid.NewGuid():N}-arm64.dmg");

        var exception = Assert.Throws<UpdateException>(() => UpdateInstaller.Start(missing, new string('0', 64), "/Applications/Tily.app/Contents/Resources/backend", 1));

        Assert.Equal($"Installeur introuvable : {missing}", exception.Message);
    }

    [Fact]
    public void Start_WhenInstallerDigestDiffers_ThenFailsInFrench()
    {
        var installer = Path.Combine(Path.GetTempPath(), $"Tily-{Guid.NewGuid():N}-arm64.dmg");
        File.WriteAllText(installer, "contenu modifié");
        try
        {
            var exception = Assert.Throws<UpdateException>(() => UpdateInstaller.Start(installer, new string('0', 64), "/Applications/Tily.app/Contents/Resources/backend", 1));

            Assert.Equal("L’installeur ne correspond plus à l’empreinte SHA-256 publiée : rien n’a été installé.", exception.Message);
        }
        finally
        {
            File.Delete(installer);
        }
    }

    [Fact]
    public void Script_WhenRendered_ThenWaitsReplacesAndRelaunches()
    {
        Assert.Contains("kill -0 \"$PID\"", UpdateInstaller.Script);
        Assert.Contains("mv \"$STAGED\" \"$APP\"", UpdateInstaller.Script);
        Assert.Contains("open -n \"$APP\"", UpdateInstaller.Script);
        Assert.DoesNotContain("hdiutil", UpdateInstaller.Script);
        Assert.DoesNotContain("ditto", UpdateInstaller.Script);
    }

    [Fact]
    public void Script_WhenRendered_ThenKeepsPreviousApplicationUntilReplaced()
    {
        Assert.Contains("mv \"$APP\" \"$PREVIOUS\"", UpdateInstaller.Script);
        Assert.Contains("mv \"$PREVIOUS\" \"$APP\"", UpdateInstaller.Script);
        Assert.DoesNotContain("rm -rf \"$APP\" && mv", UpdateInstaller.Script);
    }

    [Fact]
    public void StagedPath_WhenBundleGiven_ThenSitsBesideIt() =>
        Assert.Equal("/Applications/Tily.app.update", UpdateInstaller.StagedPath("/Applications/Tily.app"));

    [Fact]
    public void Stage_WhenNotInsideApplication_ThenFailsInFrench()
    {
        var exception = Assert.Throws<UpdateException>(() => UpdateInstaller.Stage("/tmp/Tily-1.0.0-arm64.dmg", AppContext.BaseDirectory));

        Assert.Equal("Tily ne tourne pas depuis une application installée : mise à jour impossible.", exception.Message);
    }
}
