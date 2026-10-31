namespace Tily.Core.Appearance;

public sealed record AccentPresetModel(string Name, string Color);

public static class AccentPresets
{
    public const string Sage = "#7a9f8b";
    public const string System = "#0a84ff";
    public const string Blue = "#6a96d4";

    public static readonly IReadOnlyList<AccentPresetModel> All =
    [
        new("Bleu système", System),
        new("Sauge", Sage),
        new("Bleu", Blue),
        new("Lagon", "#6fb3b0"),
        new("Mousse", "#9fbf6f"),
        new("Ambre", "#c9a65f"),
        new("Terracotta", "#c9906a"),
        new("Rose", "#c98fa5"),
        new("Lavande", "#a98fc9"),
        new("Ardoise", "#9299a1")
    ];
}
