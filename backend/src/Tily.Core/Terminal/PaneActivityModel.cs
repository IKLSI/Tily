namespace Tily.Core.Terminal;

public sealed record PaneActivityModel(string PaneId, IReadOnlyList<string> Processes);
