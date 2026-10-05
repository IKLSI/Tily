namespace Tily.Core.Agents;

public interface IAgentAdapter
{
    string Id { get; }
    PaneAgentModel? Detect(PaneProbeModel probe, AgentStateModel? reported);
}
