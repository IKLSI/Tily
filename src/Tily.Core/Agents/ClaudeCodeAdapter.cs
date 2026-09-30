namespace Tily.Core.Agents;

public sealed class ClaudeCodeAdapter : IAgentAdapter
{
    public string Id => "claude";

    public PaneAgentModel? Detect(PaneProbeModel probe, AgentStateModel? reported)
    {
        if (reported is not null && reported.Agent == Id)
        {
            return new PaneAgentModel(probe.PaneId, Id, reported.State, reported.Message, reported.Detail);
        }

        return probe.Processes.Contains(Id, StringComparer.OrdinalIgnoreCase)
            ? new PaneAgentModel(probe.PaneId, Id, AgentState.Unknown, null)
            : null;
    }
}
