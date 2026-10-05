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

        var exception = Assert.Throws<UpdateException>(() => UpdateInstaller.Start(missing, "/Applications/Tily.app/Contents/Resources/backend", 1));

        Assert.Equal($"Installeur introuvable : {missing}", exception.Message);
    }

    [Fact]
    public void Script_WhenRendered_ThenWaitsReplacesAndRelaunches()
    {
        Assert.Contains("kill -0 \"$PID\"", UpdateInstaller.Script);
        Assert.Contains("hdiutil attach", UpdateInstaller.Script);
        Assert.Contains("ditto \"$SOURCE\" \"$STAGED\"", UpdateInstaller.Script);
        Assert.Contains("open \"$APP\"", UpdateInstaller.Script);
    }
}
