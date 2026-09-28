namespace Dock.Core.Git;

public static class GitOperationNames
{
    public static string Command(GitOperationKind operation) => operation switch
    {
        GitOperationKind.Merge => "merge",
        GitOperationKind.Rebase => "rebase",
        GitOperationKind.CherryPick => "cherry-pick",
        _ => "revert"
    };

    public static string Label(GitOperationKind operation) => operation switch
    {
        GitOperationKind.Merge => "Merge",
        GitOperationKind.Rebase => "Rebase",
        GitOperationKind.CherryPick => "Cherry-pick",
        _ => "Revert"
    };

    public static string Finished(GitOperationKind operation) => $"{Label(operation)} terminé.";

    public static string Aborted(GitOperationKind operation) => $"{Label(operation)} annulé.";
}
