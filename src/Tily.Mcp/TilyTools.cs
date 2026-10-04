using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Tily.Core.Mcp;

namespace Tily.Mcp;

internal static class TilyTools
{
    public const string ServerName = "tily";
    public static readonly string Version = typeof(TilyTools).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public const string Instructions =
        "Tily est le terminal Windows dans lequel tourne cette session : workspaces, puis onglets, puis panes (splits). "
        + "Ces outils lisent et pilotent l’instance de Tily qui a ouvert ce terminal. "
        + "Le pane d’où vous êtes lancé est marqué « caller » dans tily_layout. "
        + "Préférez ces outils à une demande de copier-coller adressée à l’utilisateur.";

    private const string LayoutDescription =
        "Disposition complète de Tily : workspaces, onglets et panes, avec pour chaque pane son identifiant, son dossier, sa branche Git, son shell, "
        + "l’état de l’agent qui y tourne (Claude Code, Codex) et s’il a déjà démarré. Le pane appelant porte « caller: true », "
        + "le workspace, l’onglet et le pane affichés portent « active: true ». Un pane jamais affiché depuis le lancement de Tily n’a pas encore démarré : "
        + "son shell ne tourne pas.";

    public static McpServerPrimitiveCollection<McpServerTool> Collection() =>
    [
        McpServerTool.Create((Func<CancellationToken, Task<CallToolResult>>)LayoutAsync, new McpServerToolCreateOptions
        {
            Name = "tily_layout",
            Title = "Disposition de Tily",
            Description = LayoutDescription,
            ReadOnly = true,
            Idempotent = true,
            Destructive = false,
            OpenWorld = false
        })
    ];

    private static Task<CallToolResult> LayoutAsync(CancellationToken token) =>
        TilyConnection.CallAsync(McpLayout.Tool, null, token);
}
