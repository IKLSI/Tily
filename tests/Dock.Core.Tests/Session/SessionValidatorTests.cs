using Dock.Core.Session;
using Xunit;

namespace Dock.Core.Tests.Session;

public sealed class SessionValidatorTests
{
    [Fact]
    public void Validate_WhenInitialSession_ThenIsValid()
    {
        var session = SessionFactory.Initial();

        var result = SessionValidator.Validate(session);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNoWorkspaceAndNoActive_ThenIsValid()
    {
        var session = new SessionModel { Active = string.Empty };

        var result = SessionValidator.Validate(session);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNoWorkspaceButActiveSet_ThenFails()
    {
        var session = new SessionModel { Active = "fantome" };

        var result = SessionValidator.Validate(session);

        Assert.Equal("Workspace actif invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenClosedTabHasUnknownActivePane_ThenFails()
    {
        var session = SessionFactory.Initial();
        var closedTab = SessionFactory.Tab("C:\\", "powershell");
        closedTab.Active = "inconnu";
        session.Closed.Add(new ClosedTabModel { WorkspaceId = session.Active, WorkspaceName = "Général", Index = 0, Tab = closedTab });

        var result = SessionValidator.Validate(session);

        Assert.Equal("Pane actif invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenFavoriteEmpty_ThenFails()
    {
        var session = SessionFactory.Initial();
        session.Favorites.Add(" ");

        var result = SessionValidator.Validate(session);

        Assert.False(result.IsValid);
        Assert.Equal("Favoris invalides.", result.Error);
    }

    [Fact]
    public void Validate_WhenFavoritesDuplicated_ThenKeepsOne()
    {
        var session = SessionFactory.Initial();
        session.Favorites.AddRange(["split-x", "split-x", "new-tab"]);

        var result = SessionValidator.Validate(session);

        Assert.True(result.IsValid);
        Assert.Equal(["split-x", "new-tab"], session.Favorites);
    }

    [Fact]
    public void Validate_WhenMoreClosedTabsThanLimit_ThenKeepsMostRecent()
    {
        var session = SessionFactory.Initial();
        for (var index = 0; index <= SessionLimits.MaxClosedTabs; index++)
        {
            session.Closed.Add(new ClosedTabModel { WorkspaceId = session.Active, WorkspaceName = "Général", Index = index, Tab = SessionFactory.Tab("C:\\", "powershell") });
        }

        var result = SessionValidator.Validate(session);

        Assert.True(result.IsValid);
        Assert.Equal(SessionLimits.MaxClosedTabs, session.Closed.Count);
        Assert.Equal(SessionLimits.MaxClosedTabs, session.Closed[^1].Index);
    }

    [Fact]
    public void Validate_WhenNull_ThenFails()
    {
        var result = SessionValidator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Equal("Format de session incorrect.", result.Error);
    }

    [Fact]
    public void Validate_WhenActiveWorkspaceUnknown_ThenFails()
    {
        var session = SessionFactory.Initial();
        session.Active = "inconnu";

        var result = SessionValidator.Validate(session);

        Assert.Equal("Workspace actif invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenActivePaneUnknown_ThenFails()
    {
        var session = SessionFactory.Initial();
        session.Workspaces[0].Tabs[0].Active = "inconnu";

        var result = SessionValidator.Validate(session);

        Assert.Equal("Pane actif invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenSplitRatioOutOfRange_ThenFails()
    {
        var session = SessionFactory.Initial();
        var tab = session.Workspaces[0].Tabs[0];
        var leaf = tab.Tree;
        tab.Tree = new SplitNodeModel { Axis = "x", Ratio = 0.95, A = leaf, B = new SplitNodeModel { Pane = SessionFactory.Pane("C:\\", "powershell") } };

        var result = SessionValidator.Validate(session);

        Assert.Equal("Split invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenTreeTooDeep_ThenFails()
    {
        var session = SessionFactory.Initial();
        var tab = session.Workspaces[0].Tabs[0];
        var node = tab.Tree;
        for (var depth = 0; depth <= SessionLimits.MaxDepth; depth++)
        {
            node = new SplitNodeModel { Axis = "y", Ratio = 0.5, A = node, B = new SplitNodeModel { Pane = SessionFactory.Pane("C:\\", "powershell") } };
        }

        tab.Tree = node;

        var result = SessionValidator.Validate(session);

        Assert.Equal("Disposition invalide.", result.Error);
    }

    [Fact]
    public void Validate_WhenSidebarOutOfRange_ThenClampsIt()
    {
        var session = SessionFactory.Initial();
        session.Sidebar = 10;

        SessionValidator.Validate(session);

        Assert.Equal(SessionLimits.MinSidebarWidth, session.Sidebar);
    }

    [Fact]
    public void Validate_WhenPanelUnknown_ThenFallsBackToFiles()
    {
        var session = SessionFactory.Initial();
        session.Workspaces[0].Tabs[0].Panel = "inconnu";

        var result = SessionValidator.Validate(session);

        Assert.True(result.IsValid);
        Assert.Null(session.Workspaces[0].Tabs[0].Panel);
    }

    [Fact]
    public void Validate_WhenPanelGit_ThenKeepsIt()
    {
        var session = SessionFactory.Initial();
        session.Workspaces[0].Tabs[0].Panel = "git";

        SessionValidator.Validate(session);

        Assert.Equal("git", session.Workspaces[0].Tabs[0].Panel);
    }

    [Fact]
    public void Validate_WhenExplorerWidthOutOfRange_ThenClampsIt()
    {
        var session = SessionFactory.Initial();
        session.ExplorerWidth = 5000;

        SessionValidator.Validate(session);

        Assert.Equal(SessionLimits.MaxExplorerWidth, session.ExplorerWidth);
    }

    [Fact]
    public void Validate_WhenGitGraphMissing_ThenUsesDefaultLayout()
    {
        var session = SessionFactory.Initial();
        session.GitGraph = null;

        SessionValidator.Validate(session);

        Assert.Equal((SessionLimits.DefaultGitLabelsWidth, true, true), (session.GitGraph?.LabelsWidth, session.GitGraph?.AuthorShown, session.GitGraph?.ReferencesOpen));
    }

    [Fact]
    public void Validate_WhenGitGraphWidthsOutOfRange_ThenClampsThem()
    {
        var session = SessionFactory.Initial();
        session.GitGraph = new GitGraphLayoutModel { ReferencesWidth = 10, LabelsWidth = 5000, GraphWidth = -3, AuthorShown = false };

        SessionValidator.Validate(session);

        Assert.Equal(
            (SessionLimits.MinGitReferencesWidth, SessionLimits.MaxGitColumnWidth, SessionLimits.MinGitColumnWidth, false),
            (session.GitGraph?.ReferencesWidth, session.GitGraph?.LabelsWidth, session.GitGraph?.GraphWidth, session.GitGraph?.AuthorShown));
    }
}
