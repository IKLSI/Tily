namespace Tily.Core.Agents;

public sealed class ClaudeCodeAdapter : IAgentAdapter
{
    public const string InterruptedMessage = "Interrompu.";

    private readonly ClaudeSessionRegistry? _registry;

    public ClaudeCodeAdapter(ClaudeSessionRegistry? registry = null)
    {
        _registry = registry;
    }

    public string Id => "claude";

    public PaneAgentModel? Detect(PaneProbeModel probe, AgentStateModel? reported)
    {
        var session = _registry?.Find(probe.ProcessIds ?? []);
        var agent = reported is not null && reported.Agent == Id
            ? Reconcile(new PaneAgentModel(probe.PaneId, Id, reported.State, reported.Message, reported.Detail), reported, session)
            : session is not null || probe.Processes.Contains(Id, StringComparer.OrdinalIgnoreCase)
                ? new PaneAgentModel(probe.PaneId, Id, AgentState.Unknown, null)
                : null;
        return agent is null || session is null
            ? agent
            : agent with { SessionId = session.SessionId, SessionDirectory = session.Directory, TranscriptPath = _registry!.TranscriptPathFor(session) };
    }

    private static PaneAgentModel Reconcile(PaneAgentModel agent, AgentStateModel reported, ClaudeSessionModel? session)
    {
        var interrupted = reported.State is AgentState.Working or AgentState.Waiting
            && session is { Status: ClaudeSessionStatus.Idle, StatusUpdatedAtUtc: { } idleSince }
            && idleSince > reported.UpdatedAtUtc;
        return interrupted
            ? agent with { State = AgentState.Done, Message = InterruptedMessage, Detail = null, Interrupted = true }
            : agent;
    }
}
