using Tily.Core.StatusLog;
using Xunit;

namespace Tily.Core.Tests.StatusLog;

public sealed class UserErrorMessageTests
{
    [Fact]
    public void Of_WhenFileMissing_ThenNamesFileInFrench()
    {
        var message = UserErrorMessage.Of(new FileNotFoundException("Could not find file.", "/dépôt/a.txt"));

        Assert.Equal("Fichier introuvable : /dépôt/a.txt", message);
    }

    [Fact]
    public void Of_WhenAccessDenied_ThenStartsInFrenchAndKeepsSystemDetail()
    {
        var message = UserErrorMessage.Of(new UnauthorizedAccessException("Access to the path is denied."));

        Assert.Equal("Accès refusé (Access to the path is denied)", message);
    }

    [Fact]
    public void Of_WhenGenericInputOutputError_ThenStartsInFrench()
    {
        var message = UserErrorMessage.Of(new IOException("The process cannot access the file."));

        Assert.Equal("Erreur de lecture ou d’écriture (The process cannot access the file)", message);
    }

    [Fact]
    public void Of_WhenTilyException_ThenKeepsFrenchMessage()
    {
        var message = UserErrorMessage.Of(new InvalidOperationException("Réglages manquants."));

        Assert.Equal("Réglages manquants.", message);
    }
}
