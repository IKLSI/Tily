namespace Tily.Core.Session;

public static class SplitTree
{
    public static IEnumerable<PaneModel> Panes(SplitNodeModel? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node.IsLeaf)
        {
            yield return node.Pane!;
            yield break;
        }

        foreach (var pane in Panes(node.A).Concat(Panes(node.B)))
        {
            yield return pane;
        }
    }
}
