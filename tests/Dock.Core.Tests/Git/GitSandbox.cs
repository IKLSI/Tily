using Dock.Core.Git;

namespace Dock.Core.Tests.Git;

public sealed class GitSandbox : IDisposable
{
    private const string Configuration = "[user]\n\tname = Dock Tests\n\temail = tests@dock.local\n[commit]\n\tgpgsign = false\n[tag]\n\tgpgsign = false\n[init]\n\tdefaultBranch = main\n[core]\n\tautocrlf = false\n\thooksPath = aucun-hook\n";

    public GitSandbox()
    {
        Root = Path.Combine(Path.GetTempPath(), "dock-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        var configuration = Path.Combine(Root, "gitconfig");
        File.WriteAllText(configuration, Configuration);
        Runner = new GitRunner(new Dictionary<string, string?> { ["GIT_CONFIG_GLOBAL"] = configuration, ["GIT_CONFIG_NOSYSTEM"] = "1" });
        var work = Path.Combine(Root, "dépôt avec espaces");
        Directory.CreateDirectory(work);
        GitIn(work, "init", "-q", "-b", "main");
        Work = GitRepository.Locate(Runner, work)!.Root;
    }

    public string Root { get; }

    public string Work { get; }

    public GitRunner Runner { get; }

    public GitRepository Repository => new(Runner, GitRepository.Locate(Runner, Work)!);

    public string Git(params string[] arguments) => GitIn(Work, arguments);

    public string GitIn(string directory, params string[] arguments)
    {
        var output = Runner.Run(directory, arguments);
        return output.Succeeded ? output.Output : throw new InvalidOperationException($"git {string.Join(' ', arguments)} : {output.Details}");
    }

    public void Write(string relativePath, string content)
    {
        var path = Path.Combine(Work, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public string Read(string relativePath) => File.ReadAllText(Path.Combine(Work, relativePath));

    public bool Exists(string relativePath) => File.Exists(Path.Combine(Work, relativePath));

    public string Commit(string message, params (string Path, string Content)[] files)
    {
        foreach (var (path, content) in files)
        {
            Write(path, content);
        }

        Git("add", "-A");
        Git("commit", "-q", "-m", message);
        return Head();
    }

    public string Head() => Git("rev-parse", "HEAD").Trim();

    public string Branch() => Git("symbolic-ref", "--short", "HEAD").Trim();

    public string CreateRemote()
    {
        var remote = Path.Combine(Root, "distant.git");
        GitIn(Root, "init", "-q", "--bare", "-b", "main", remote);
        Git("remote", "add", "origin", remote);
        return remote;
    }

    public string Clone(string remote, string name)
    {
        var clone = Path.Combine(Root, name);
        GitIn(Root, "clone", "-q", remote, clone);
        return clone;
    }

    public void Dispose()
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(Root, true);
    }
}
