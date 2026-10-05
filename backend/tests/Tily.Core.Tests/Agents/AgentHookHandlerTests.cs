using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tily.Core.Agents;
using Xunit;

namespace Tily.Core.Tests.Agents;

public sealed class AgentHookHandlerTests : IDisposable
{
    private const string PaneId = "pane-hook";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _home;
    private readonly AgentHookHandler _handler;

    public AgentHookHandlerTests()
    {
        _home = Path.Combine(_directory, "maison");
        _handler = new AgentHookHandler(PaneId, _directory, _home);
    }

    [Fact]
    public void Handle_WhenBashPermissionRequested_ThenWritesCommandAsDetail()
    {
        var state = Run("{ \"hook_event_name\": \"PermissionRequest\", \"tool_name\": \"Bash\", \"tool_input\": { \"command\": \"git push --force origin main\", \"description\": \"Pousser\" } }");

        Assert.Equal("Autorisation demandée : Bash", state["message"]!.GetValue<string>());
        Assert.Equal("git push --force origin main", state["detail"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenQuestionAsked_ThenWritesFirstQuestionWithAccents()
    {
        var state = Run("{ \"hook_event_name\": \"PreToolUse\", \"tool_name\": \"AskUserQuestion\", \"tool_input\": { \"questions\": [ { \"question\": \"Quelle stratégie adopter à l’étape 2 ?\" } ] } }");

        Assert.Equal("Quelle stratégie adopter à l’étape 2 ?", state["detail"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenUnknownToolPermissionRequested_ThenWritesInputAsJson()
    {
        var state = Run("{ \"hook_event_name\": \"PermissionRequest\", \"tool_name\": \"mcp__jira__create\", \"tool_input\": { \"summary\": \"Bug\" } }");

        Assert.Equal("{\"summary\":\"Bug\"}", state["detail"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenStopped_ThenWritesLastAssistantTextFromTranscript()
    {
        Directory.CreateDirectory(_directory);
        var transcript = Path.Combine(_directory, "session.jsonl");
        File.WriteAllLines(transcript,
        [
            "{\"type\":\"user\",\"message\":{\"role\":\"user\",\"content\":\"Corrige le bug\"}}",
            "{\"isSidechain\":false,\"message\":{\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"Le bug est corrigé et testé.\"}]},\"type\":\"assistant\"}",
            "{\"isSidechain\":true,\"message\":{\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"Sous-agent\"}]},\"type\":\"assistant\"}",
            "{\"isSidechain\":false,\"message\":{\"role\":\"assistant\",\"content\":[{\"type\":\"tool_use\",\"name\":\"Bash\"}]},\"type\":\"assistant\"}"
        ], new UTF8Encoding(false));

        var state = Run(JsonSerializer.Serialize(new Dictionary<string, string> { ["hook_event_name"] = "Stop", ["transcript_path"] = transcript }));

        Assert.Equal("done", state["state"]!.GetValue<string>());
        Assert.Equal("Le bug est corrigé et testé.", state["detail"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenStoppedWithLastAssistantMessage_ThenPrefersIt()
    {
        var state = Run("{ \"hook_event_name\": \"Stop\", \"last_assistant_message\": \"Terminé : 3 fichiers modifiés.\", \"transcript_path\": \"/absent.jsonl\" }");

        Assert.Equal("Terminé : 3 fichiers modifiés.", state["detail"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenWaitingNotificationAfterPermissionRequest_ThenKeepsPermissionMessage()
    {
        Run("{ \"hook_event_name\": \"PermissionRequest\", \"tool_name\": \"Bash\", \"tool_input\": { \"command\": \"rm -rf dist\" } }");

        var state = Run("{ \"hook_event_name\": \"Notification\", \"notification_type\": \"permission_prompt\" }");

        Assert.Equal("Autorisation demandée : Bash", state["message"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenSessionEnds_ThenDeletesStateFile()
    {
        Run("{ \"hook_event_name\": \"UserPromptSubmit\" }");

        _handler.Handle("{ \"hook_event_name\": \"SessionEnd\" }");

        Assert.False(File.Exists(_handler.StateFile));
    }

    [Fact]
    public void Handle_WhenClaudeOpensHtmlWithHomePath_ThenRequestsPreviewAndDeniesCommand()
    {
        var page = Page(Path.Combine("maison", "relecture.html"));

        var output = _handler.Handle(OpenPayload("open ~/relecture.html", "/"));

        Assert.Equal("deny", JsonNode.Parse(output!)!["hookSpecificOutput"]!["permissionDecision"]!.GetValue<string>());
        Assert.Equal(page, RequestedPaths().Single());
    }

    [Fact]
    public void Handle_WhenClaudeOpensRelativeHtmlWithApplication_ThenResolvesItFromWorkingDirectory()
    {
        var page = Page("plan.html");

        _handler.Handle(OpenPayload("open -a \"Google Chrome\" plan.html", _directory));

        Assert.Equal(page, RequestedPaths().Single());
    }

    [Theory]
    [InlineData("open rapport.pdf")]
    [InlineData("open absent.html")]
    [InlineData("echo \"open plan.html\"")]
    [InlineData("git status # ; open plan.html")]
    [InlineData("cat > notes.md <<'EOF'\nopen plan.html\nEOF")]
    public void Handle_WhenCommandOpensNoPageToPreview_ThenLetsItRun(string command)
    {
        Page("plan.html");
        Page("rapport.pdf");

        var output = _handler.Handle(OpenPayload(command, _directory));

        Assert.Equal((null, 0), (output, RequestedPaths().Count));
    }

    [Fact]
    public void Handle_WhenCompoundCommandOpensHtmlAfterCd_ThenPreviewsItFromThatFolder()
    {
        var page = Page(Path.Combine("Projet Terminal", "b.html"));

        var output = _handler.Handle(OpenPayload("cd \"Projet Terminal\" && git check-ignore -q x ; open \"b.html\"", _directory));

        Assert.Equal(page, RequestedPaths().Single());
        Assert.EndsWith("relance le reste de la commande sans ce `open`.", Reason(output));
    }

    [Theory]
    [InlineData("git status ; open \"{page}\"")]
    [InlineData("open \"plan.html\" && echo ok")]
    [InlineData("echo ok 2>&1 || open plan.html")]
    [InlineData("cat > notes.md <<-EOF\n\topen rapport.html\n\tEOF\nopen plan.html")]
    public void Handle_WhenCompoundCommandOpensExistingHtml_ThenPreviewsItAndDeniesCommand(string command)
    {
        var page = Page("plan.html");

        var output = _handler.Handle(OpenPayload(command.Replace("{page}", page), _directory));

        Assert.Equal(page, RequestedPaths().Single());
        Assert.Equal("deny", JsonNode.Parse(output!)!["hookSpecificOutput"]!["permissionDecision"]!.GetValue<string>());
    }

    [Fact]
    public void Handle_WhenCompoundCommandOpensHtmlNotYetGenerated_ThenDeniesWithStandaloneOpenToRun()
    {
        var page = Path.Combine(_directory, "rapport.html");

        var output = _handler.Handle(OpenPayload("node build.js && open \"rapport.html\"", _directory));

        Assert.Empty(RequestedPaths());
        Assert.Contains($"puis lance `open \"{page}\"` seul", Reason(output));
    }

    private string Page(string name)
    {
        var path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<h1>Plan</h1>");
        return path;
    }

    private static string OpenPayload(string command, string workingDirectory) =>
        JsonSerializer.Serialize(new { hook_event_name = "PreToolUse", tool_name = "Bash", cwd = workingDirectory, tool_input = new { command } });

    private static string Reason(string? output) =>
        JsonNode.Parse(output!)!["hookSpecificOutput"]!["permissionDecisionReason"]!.GetValue<string>();

    private List<string> RequestedPaths() =>
        Directory.Exists(_handler.PreviewDirectory)
            ? Directory.EnumerateFiles(_handler.PreviewDirectory, "*.json").Select(file => JsonNode.Parse(File.ReadAllText(file, Encoding.UTF8))!["path"]!.GetValue<string>()).ToList()
            : [];

    private JsonNode Run(string payload)
    {
        _handler.Handle(payload);
        return JsonNode.Parse(File.ReadAllText(_handler.StateFile, Encoding.UTF8))!;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
