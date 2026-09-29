using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitAutoFetchScheduleTests
{
    private const string Root = @"C:\Files\Projects\dock";
    private static readonly DateTimeOffset Start = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryStart_WhenRepositoryNeverFetched_ThenStarts()
    {
        var schedule = new GitAutoFetchSchedule();

        var started = schedule.TryStart(Root, Start);

        Assert.True(started, "Le premier fetch automatique d’un dépôt doit partir.");
    }

    [Fact]
    public void TryStart_WhenFetchedLessThanIntervalAgo_ThenSkips()
    {
        var schedule = new GitAutoFetchSchedule();
        schedule.TryStart(Root, Start);

        var started = schedule.TryStart(Root, Start + GitAutoFetchSchedule.MinimumInterval - TimeSpan.FromSeconds(1));

        Assert.False(started, "Un second fetch automatique dans l’intervalle doit être ignoré.");
    }

    [Fact]
    public void TryStart_WhenIntervalElapsed_ThenStartsAgain()
    {
        var schedule = new GitAutoFetchSchedule();
        schedule.TryStart(Root, Start);

        var started = schedule.TryStart(Root, Start + GitAutoFetchSchedule.MinimumInterval);

        Assert.True(started, "Le fetch automatique doit repartir une fois l’intervalle écoulé.");
    }

    [Fact]
    public void TryStart_WhenOtherRepositoryFetched_ThenStarts()
    {
        var schedule = new GitAutoFetchSchedule();
        schedule.TryStart(Root, Start);

        var started = schedule.TryStart(@"C:\Files\Projects\autre", Start);

        Assert.True(started, "L’intervalle est propre à chaque dépôt.");
    }

    [Fact]
    public void TryStart_WhenRootDiffersOnlyByCase_ThenSkips()
    {
        var schedule = new GitAutoFetchSchedule();
        schedule.TryStart(Root, Start);

        var started = schedule.TryStart(Root.ToUpperInvariant(), Start);

        Assert.False(started, "Les chemins Windows ne tiennent pas compte de la casse.");
    }

    [Fact]
    public void TryStart_WhenManualFetchRecorded_ThenSkips()
    {
        var schedule = new GitAutoFetchSchedule();
        schedule.Record(Root, Start);

        var started = schedule.TryStart(Root, Start + TimeSpan.FromMinutes(1));

        Assert.False(started, "Un fetch manuel récent rend le fetch automatique inutile.");
    }
}
