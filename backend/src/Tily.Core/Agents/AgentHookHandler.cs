using System.Text.Json;
using System.Text.Json.Nodes;
using Tily.Core.Session;

namespace Tily.Core.Agents;

public sealed class AgentHookHandler
{
    private const string Agent = "claude";
    private const string WaitingState = "waiting";
    private const string PermissionPrompt = "permission_prompt";
    private static readonly string[] WaitingNotifications = [PermissionPrompt, "elicitation_dialog", "agent_needs_input"];
    private static readonly JsonSerializerOptions OutputOptions = new() { Encoder = SessionRepository.JsonOptions.Encoder };

    private readonly string _paneId;
    private readonly string _dataDirectory;
    private readonly string _home;

    public AgentHookHandler(string paneId, string dataDirectory, string home)
    {
        _paneId = paneId;
        _dataDirectory = dataDirectory;
        _home = home;
    }

    public string StateFile => Path.Combine(_dataDirectory, "agents", _paneId + ".json");

    public string PreviewDirectory => Path.Combine(_dataDirectory, "previews");

    public string? Handle(string input)
    {
        JsonObject hook;
        try
        {
            if (JsonNode.Parse(input) is not JsonObject parsed)
            {
                return null;
            }

            hook = parsed;
        }
        catch (JsonException)
        {
            return null;
        }

        var eventName = AgentHookInput.Text(hook["hook_event_name"]);
        if (eventName == "SessionEnd")
        {
            TryDelete(StateFile);
            return null;
        }

        var toolName = AgentHookInput.Text(hook["tool_name"]);
        if (eventName == "PreToolUse" && toolName == "Bash")
        {
            return PreviewHtml(hook);
        }

        var state = StateOf(eventName, toolName, hook);
        if (state is null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(StateFile)!);
        var json = new JsonObject { ["agent"] = Agent, ["state"] = state.Value.State, ["message"] = state.Value.Message, ["detail"] = state.Value.Detail };
        AtomicFile.Write(StateFile, json.ToJsonString(OutputOptions));
        return null;
    }

    private (string State, string? Message, string? Detail)? StateOf(string? eventName, string? toolName, JsonObject hook) => eventName switch
    {
        "SessionStart" => ("unknown", null, null),
        "UserPromptSubmit" or "PostToolUse" => ("working", null, null),
        "PreToolUse" when toolName == "AskUserQuestion" => (WaitingState, "Question posée.", AgentHookInput.ToolDetail(hook["tool_input"])),
        "Stop" => ("done", null, AgentHookInput.LastAssistantText(hook)),
        "StopFailure" => ("error", "Erreur signalée par Claude Code.", null),
        "PermissionRequest" => (WaitingState, $"Autorisation demandée : {toolName}", AgentHookInput.ToolDetail(hook["tool_input"])),
        "Notification" => NotificationState(AgentHookInput.Text(hook["notification_type"])),
        _ => null
    };

    private (string State, string? Message, string? Detail)? NotificationState(string? type)
    {
        if (type is null || !WaitingNotifications.Contains(type) || AlreadyWaiting())
        {
            return null;
        }

        return (WaitingState, type == PermissionPrompt ? "Autorisation demandée." : "Saisie attendue.", null);
    }

    private bool AlreadyWaiting()
    {
        try
        {
            return File.Exists(StateFile) && JsonNode.Parse(File.ReadAllText(StateFile)) is JsonObject current && AgentHookInput.Text(current["state"]) == WaitingState;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string? PreviewHtml(JsonObject hook)
    {
        var command = AgentHookInput.Text(hook["tool_input"]?["command"]) ?? string.Empty;
        var opening = HtmlOpeningFinder.Find(command, AgentHookInput.Text(hook["cwd"]), _home);
        if (opening is null || (opening.Alone && !opening.Exists))
        {
            return null;
        }

        if (opening.Exists && !TrySubmitPreview(opening.Target!))
        {
            return null;
        }

        var output = new JsonObject
        {
            ["hookSpecificOutput"] = new JsonObject
            {
                ["hookEventName"] = "PreToolUse",
                ["permissionDecision"] = "deny",
                ["permissionDecisionReason"] = ReasonFor(opening)
            }
        };
        return output.ToJsonString(OutputOptions);
    }

    private static string ReasonFor(HtmlOpeningModel opening)
    {
        if (opening.Alone)
        {
            return $"Tily a ouvert {opening.Target} dans son aperçu HTML, à côté du terminal : l’utilisateur le relit dans Tily, inutile de l’ouvrir autrement.";
        }

        if (opening.Exists)
        {
            return $"Tily a ouvert {opening.Target} dans son aperçu HTML, à côté du terminal ; relance le reste de la commande sans ce `open`.";
        }

        var page = opening.Target ?? opening.Path;
        return $"Tily ouvre les pages HTML dans son aperçu, mais {page} est introuvable avant la commande : relance le reste de la commande sans ce `open`, puis lance `open \"{page}\"` seul pour que Tily l’ouvre.";
    }

    private bool TrySubmitPreview(string path)
    {
        try
        {
            Directory.CreateDirectory(PreviewDirectory);
            var name = $"{_paneId}-{Guid.NewGuid():N}";
            var temporary = Path.Combine(PreviewDirectory, name + ".tmp");
            var json = new JsonObject { ["pane"] = _paneId, ["path"] = path };
            File.WriteAllText(temporary, json.ToJsonString(OutputOptions));
            File.Move(temporary, Path.Combine(PreviewDirectory, name + ".json"), true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
