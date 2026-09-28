namespace Dock.Core.Agents;

public sealed class CodexAdapter : IAgentAdapter
{
    public string Id => "codex";

    public PaneAgentModel? Detect(PaneProbeModel probe, AgentStateModel? reported) =>
        probe.Processes.Contains(Id, StringComparer.OrdinalIgnoreCase)
            ? new PaneAgentModel(probe.PaneId, Id, AgentState.Unknown, null)
            : null;
}
