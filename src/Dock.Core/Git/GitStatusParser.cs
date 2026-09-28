namespace Dock.Core.Git;

public static class GitStatusParser
{
    public const int MaxFilesPerGroup = 1000;

    private const string InitialCommit = "(initial)";
    private const string DetachedHead = "(detached)";

    public static GitStatusModel Parse(string output, int limit = MaxFilesPerGroup)
    {
        var head = new HeadBuilder();
        var staged = new List<GitFileChangeModel>();
        var unstaged = new List<GitFileChangeModel>();
        var conflicts = new List<GitConflictModel>();
        var fields = output.Split('\0');
        for (var index = 0; index < fields.Length; index++)
        {
            var entry = fields[index];
            if (entry.Length < 2)
            {
                continue;
            }

            switch (entry[0])
            {
                case '#':
                    head.Read(entry);
                    break;
                case '1':
                    AddOrdinary(entry.Split(' ', 9), null, staged, unstaged);
                    break;
                case '2':
                    AddOrdinary(entry.Split(' ', 10), index + 1 < fields.Length ? fields[++index] : null, staged, unstaged);
                    break;
                case 'u':
                    AddConflict(entry.Split(' ', 11), conflicts);
                    break;
                case '?':
                    unstaged.Add(new GitFileChangeModel(entry[2..], null, GitChangeKind.Untracked));
                    break;
            }
        }

        return new GitStatusModel(head.Build(), staged.Take(limit).ToList(), unstaged.Take(limit).ToList(), conflicts, staged.Count, unstaged.Count);
    }

    private static void AddOrdinary(string[] parts, string? originalPath, List<GitFileChangeModel> staged, List<GitFileChangeModel> unstaged)
    {
        if (parts.Length < 9)
        {
            return;
        }

        var path = parts[^1];
        var indexKind = KindOf(parts[1][0]);
        var workTreeKind = KindOf(parts[1][1]);
        if (indexKind is not null)
        {
            staged.Add(new GitFileChangeModel(path, indexKind is GitChangeKind.Renamed or GitChangeKind.Copied ? originalPath : null, indexKind.Value));
        }

        if (workTreeKind is not null)
        {
            unstaged.Add(new GitFileChangeModel(path, null, workTreeKind.Value));
        }
    }

    private static void AddConflict(string[] parts, List<GitConflictModel> conflicts)
    {
        if (parts.Length < 11)
        {
            return;
        }

        var kind = parts[1] switch
        {
            "DD" => GitConflictKind.BothDeleted,
            "AU" => GitConflictKind.AddedByUs,
            "UD" => GitConflictKind.DeletedByThem,
            "UA" => GitConflictKind.AddedByThem,
            "DU" => GitConflictKind.DeletedByUs,
            "AA" => GitConflictKind.BothAdded,
            _ => GitConflictKind.BothModified
        };
        conflicts.Add(new GitConflictModel(parts[^1], kind));
    }

    private static GitChangeKind? KindOf(char code) => code switch
    {
        'M' => GitChangeKind.Modified,
        'T' => GitChangeKind.TypeChanged,
        'A' => GitChangeKind.Added,
        'D' => GitChangeKind.Deleted,
        'R' => GitChangeKind.Renamed,
        'C' => GitChangeKind.Copied,
        _ => null
    };

    private sealed class HeadBuilder
    {
        private string? _sha;
        private string? _branch;
        private bool _detached;
        private bool _unborn;
        private string? _upstream;
        private int _ahead;
        private int _behind;

        public void Read(string header)
        {
            var parts = header.Split(' ', 3);
            if (parts.Length < 3)
            {
                return;
            }

            var value = parts[2];
            switch (parts[1])
            {
                case "branch.oid":
                    _unborn = value == InitialCommit;
                    _sha = _unborn ? null : value;
                    break;
                case "branch.head":
                    _detached = value == DetachedHead;
                    _branch = _detached ? null : value;
                    break;
                case "branch.upstream":
                    _upstream = value;
                    break;
                case "branch.ab":
                    var counts = value.Split(' ');
                    _ahead = counts.Length > 0 && int.TryParse(counts[0].TrimStart('+'), out var ahead) ? ahead : 0;
                    _behind = counts.Length > 1 && int.TryParse(counts[1].TrimStart('-'), out var behind) ? behind : 0;
                    break;
            }
        }

        public GitHeadModel Build() => new(_branch, _sha, _detached, _unborn, _upstream, _ahead, _behind);
    }
}
