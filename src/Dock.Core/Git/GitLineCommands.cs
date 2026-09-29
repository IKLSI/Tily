namespace Dock.Core.Git;

public enum GitLineAction
{
    Stage,
    Unstage,
    Discard
}

public sealed record GitLineRequestModel(string? Path, bool Untracked, string? Fingerprint, IReadOnlyList<GitHunkSelectionModel> Selection);

public static class GitLineCommands
{
    private const string Failure = "La sélection n’a pas pu être appliquée : ajoutez les lignes voisines ou le chunk entier, puis recommencez.";

    public static GitOutcomeModel Apply(GitRepository repository, GitLineAction action, GitLineRequestModel request, bool confirmed)
    {
        if (action == GitLineAction.Discard)
        {
            GitPaths.RequireConfirmation(confirmed);
        }

        var path = GitNames.RequireRelativePath(request.Path);
        var source = action == GitLineAction.Unstage ? GitDiffSource.Staged : GitDiffSource.Unstaged;
        var diff = GitDiffReader.ReadRaw(repository, source, path, request.Untracked && source == GitDiffSource.Unstaged);
        if (diff is null || request.Fingerprint is null || GitDiffReader.Fingerprint(diff) != request.Fingerprint)
        {
            throw new GitCommandException("Le diff a changé depuis son affichage : vérifiez la sélection puis recommencez.", string.Empty);
        }

        var patch = GitPatchBuilder.Build(diff, request.Selection, action != GitLineAction.Stage);
        var count = GitPatchBuilder.CountLines(request.Selection);
        switch (action)
        {
            case GitLineAction.Stage:
                ApplyPatch(repository, patch, "--cached");
                return new GitOutcomeModel($"Stage de {GitText.Count(count, "ligne", "lignes")} : {path}");
            case GitLineAction.Unstage:
                ApplyPatch(repository, patch, "--cached", "--reverse");
                return new GitOutcomeModel($"Unstage de {GitText.Count(count, "ligne", "lignes")} : {path}");
            default:
                return Discard(repository, path, patch, count);
        }
    }

    private static GitOutcomeModel Discard(GitRepository repository, string path, string patch, int count)
    {
        string[] Present() => File.Exists(repository.FullPath(path)) ? [path] : [];
        var before = GitPaths.Hash(repository, Present(), true);
        ApplyPatch(repository, patch, "--reverse");
        var after = GitPaths.Hash(repository, Present(), false);
        var record = new GitUndoRecordModel
        {
            Kind = GitUndoKind.Discard,
            Label = $"Abandon de {GitText.Count(count, "ligne", "lignes")} de « {path} »",
            Files = [new GitDiscardedFileModel(path, before.GetValueOrDefault(path), after.GetValueOrDefault(path))]
        };
        return new GitOutcomeModel($"{GitText.Count(count, "ligne abandonnée", "lignes abandonnées")} : {path}", Undo: record);
    }

    private static void ApplyPatch(GitRepository repository, string patch, params string[] options) =>
        GitRepository.Require(repository.Run(new GitRunOptionsModel(Input: patch), ["apply", .. options, "--whitespace=nowarn", "-"]), Failure);
}
