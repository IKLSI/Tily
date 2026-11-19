using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Tily.Core.Mcp;
using Tily.Core.StatusLog;

namespace Tily.Mcp;

internal static class TilyBrowserTools
{
    private const string Target =
        "Sans pane, l’outil vise votre dernier pane navigateur, sinon le seul pane navigateur de votre onglet ou de Tily ; "
        + "les panes navigateur portent kind: \"browser\" dans tily_layout.";

    private const string OpenDescription =
        "Ouvre dans Tily un pane navigateur, à côté de votre pane (right), en dessous (down) ou dans un nouvel onglet (tab), et y charge une adresse "
        + "(localhost:5173 s’ouvre en http). Son onglet est affiché pour que l’utilisateur voie la page ; le focus clavier ne change pas sans focus. "
        + "Le pane vous appartient. Rend le résultat du chargement (statut HTTP, titre, nombre d’erreurs). "
        + "Si vous avez déjà un pane navigateur, préférez tily_browser_navigate. Les cookies et sessions de connexion sont gardés d’un lancement à l’autre.";

    private const string NavigateDescription = "Charge une adresse dans un pane navigateur de Tily et attend la fin du chargement (30 s au plus) : statut HTTP, titre, erreurs. " + Target;

    private const string ReloadDescription = "Recharge la page d’un pane navigateur de Tily et attend la fin du chargement : statut HTTP, titre, erreurs. " + Target;

    private const string ResizeDescription = "Affiche la page d’un pane navigateur en largeur mobile (colonne de 390 px, émulation mobile) ou desktop (largeur du pane). " + Target;

    private const string ConsoleDescription =
        "Console d’un pane navigateur de Tily, collectée en continu : journaux, avertissements, erreurs, exceptions et rejets de promesse non gérés, "
        + "avec l’adresse et la ligne d’origine, du plus ancien au plus récent. level donne le niveau minimal ; sinceLoad ne garde que le chargement en cours "
        + "(utile après tily_browser_reload). " + Target;

    private const string NetworkDescription =
        "Requêtes réseau terminées d’un pane navigateur de Tily : méthode, adresse, type, statut, durée, échec, et corps des réponses en erreur (2 000 caractères au plus). "
        + "failedOnly ne garde que les statuts ≥ 400 et les échecs. " + Target;

    private const string ScreenshotDescription =
        "Capture PNG de la page d’un pane navigateur de Tily, rendue en viewport desktop (1440 × 900) ou mobile (390 × 844, échelle 2) quelle que soit la taille du pane, "
        + "même si son onglet n’est pas affiché. fullPage capture toute la hauteur (16 000 px au plus). path enregistre aussi le PNG (chemin relatif à votre dossier). " + Target;

    private const string SnapshotDescription =
        "Liste les éléments interactifs visibles de la page d’un pane navigateur de Tily (liens, boutons, champs, listes, cases) avec leur rôle, leur nom, leur valeur et leur état, "
        + "chacun sous une référence (e12) utilisable par tily_browser_click, tily_browser_type et tily_browser_select. Les références sont renumérotées à chaque snapshot : "
        + "refaites-en un après toute navigation ou tout changement de la page. " + Target;

    private const string ClickDescription =
        "Clique au centre d’un élément d’un pane navigateur de Tily (vrai clic de souris, événements de confiance). Fait défiler l’élément dans la vue et refuse s’il est désactivé ou masqué. "
        + "Rend l’adresse et le titre 300 ms après le clic. Sur un pane ouvert par l’utilisateur, Tily lui demande son accord. " + Target;

    private const string TypeDescription =
        "Saisit du texte dans un champ d’un pane navigateur de Tily (input, textarea, contenteditable). clear remplace le contenu existant ; submit valide avec Entrée. "
        + "Sur un pane ouvert par l’utilisateur, Tily lui demande son accord. " + Target;

    private const string PressDescription =
        "Presse une touche ou une combinaison dans la page d’un pane navigateur de Tily, sur l’élément qui a le focus (Enter, Tab, Escape, Backspace, flèches, Ctrl+A, Cmd+Enter…). "
        + "Sur un pane ouvert par l’utilisateur, Tily lui demande son accord. " + Target;

    private const string SelectDescription =
        "Choisit une option d’une liste déroulante native (select) d’un pane navigateur de Tily, par valeur ou par libellé. Pour une liste personnalisée, utilisez tily_browser_click. "
        + "Sur un pane ouvert par l’utilisateur, Tily lui demande son accord. " + Target;

    private const string WaitForDescription =
        "Attend dans un pane navigateur de Tily qu’un texte apparaisse dans la page, qu’un élément (ref d’un snapshot) soit visible ou que l’adresse contienne un fragment, "
        + "même à travers une navigation. gone attend l’inverse (disparition). Une seule condition à la fois ; timeoutMs de 100 à 30 000 (10 000 par défaut). " + Target;

    private const string EvaluateDescription =
        "Exécute du JavaScript dans la page d’un pane navigateur de Tily et rend la valeur de l’expression (promesses attendues, 10 000 caractères au plus). "
        + "Le code s’exécute dans la session de la page, éventuellement connectée : à réserver à ce que les autres outils ne permettent pas. "
        + "Sur un pane ouvert par l’utilisateur, Tily lui demande son accord. " + Target;

    private const string PaneDescription = "Identifiant du pane navigateur, donné par tily_layout.";
    private const string ViewportDescription = "desktop ou mobile.";

    public static IEnumerable<McpServerTool> Tools() =>
    [
        McpServerTool.Create((Func<string?, string?, string, string?, bool, CancellationToken, Task<CallToolResult>>)OpenAsync, Options("tily_browser_open", "Ouvrir un navigateur", OpenDescription, false, false)),
        McpServerTool.Create((Func<string, string?, CancellationToken, Task<CallToolResult>>)NavigateAsync, Options("tily_browser_navigate", "Naviguer", NavigateDescription, false, false)),
        McpServerTool.Create((Func<string?, CancellationToken, Task<CallToolResult>>)ReloadAsync, Options("tily_browser_reload", "Recharger la page", ReloadDescription, false, false)),
        McpServerTool.Create((Func<string, string?, CancellationToken, Task<CallToolResult>>)ResizeAsync, Options("tily_browser_resize", "Largeur mobile ou desktop", ResizeDescription, false, true)),
        McpServerTool.Create((Func<string?, string?, bool, int, CancellationToken, Task<CallToolResult>>)ConsoleAsync, Options("tily_browser_console", "Console du navigateur", ConsoleDescription, true, true)),
        McpServerTool.Create((Func<string?, bool, bool, int, CancellationToken, Task<CallToolResult>>)NetworkAsync, Options("tily_browser_network", "Réseau du navigateur", NetworkDescription, true, true)),
        McpServerTool.Create((Func<string?, string?, bool, string?, CancellationToken, Task<CallToolResult>>)ScreenshotAsync, Options("tily_browser_screenshot", "Capture du navigateur", ScreenshotDescription, true, true)),
        McpServerTool.Create((Func<string?, CancellationToken, Task<CallToolResult>>)SnapshotAsync, Options("tily_browser_snapshot", "Éléments interactifs de la page", SnapshotDescription, true, true)),
        McpServerTool.Create((Func<string, string?, CancellationToken, Task<CallToolResult>>)ClickAsync, Options("tily_browser_click", "Cliquer", ClickDescription, false, false)),
        McpServerTool.Create((Func<string, string, string?, bool, bool, CancellationToken, Task<CallToolResult>>)TypeAsync, Options("tily_browser_type", "Saisir du texte", TypeDescription, false, false)),
        McpServerTool.Create((Func<string, string?, CancellationToken, Task<CallToolResult>>)PressAsync, Options("tily_browser_press", "Presser une touche", PressDescription, false, false)),
        McpServerTool.Create((Func<string, string, string?, CancellationToken, Task<CallToolResult>>)SelectAsync, Options("tily_browser_select", "Choisir une option", SelectDescription, false, true)),
        McpServerTool.Create((Func<string?, string?, string?, bool, int, string?, CancellationToken, Task<CallToolResult>>)WaitForAsync, Options("tily_browser_wait_for", "Attendre dans la page", WaitForDescription, true, true)),
        McpServerTool.Create((Func<string, string?, CancellationToken, Task<CallToolResult>>)EvaluateAsync, Options("tily_browser_evaluate", "Exécuter du JavaScript", EvaluateDescription, false, false))
    ];

    private static McpServerToolCreateOptions Options(string name, string title, string description, bool readOnly, bool idempotent) => new()
    {
        Name = name,
        Title = title,
        Description = description,
        ReadOnly = readOnly,
        Idempotent = idempotent,
        Destructive = false,
        OpenWorld = true
    };

    private static Task<CallToolResult> OpenAsync(
        [Description("Adresse à charger ; absente : page vide.")] string? url = null,
        [Description("Identifiant du pane à côté duquel ouvrir le navigateur ; par défaut, le vôtre.")] string? pane = null,
        [Description("right : côte à côte ; down : en dessous ; tab : nouvel onglet.")] string placement = "right",
        [Description(ViewportDescription + " Par défaut : desktop.")] string? viewport = null,
        [Description("Donner le focus clavier au navigateur.")] bool focus = false,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Open, () => new { url = McpBrowser.Url(url), pane, placement = McpBrowser.Placement(placement), viewport = McpBrowser.Viewport(viewport), focus }, token);

    private static Task<CallToolResult> NavigateAsync(
        [Description("Adresse à charger, par exemple localhost:5173/equipe.")] string url,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Navigate, () => new { url = McpBrowser.Url(url) ?? throw new InvalidOperationException("Indiquez l’adresse à charger."), pane }, token);

    private static Task<CallToolResult> ReloadAsync([Description(PaneDescription)] string? pane = null, CancellationToken token = default) =>
        TilyConnection.CallAsync(McpBrowser.Reload, new { pane }, token);

    private static Task<CallToolResult> ResizeAsync(
        [Description(ViewportDescription)] string viewport,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Resize, () => new { viewport = McpBrowser.Viewport(viewport) ?? throw new InvalidOperationException("Indiquez la largeur : desktop ou mobile."), pane }, token);

    private static Task<CallToolResult> ConsoleAsync(
        [Description(PaneDescription)] string? pane = null,
        [Description("Niveau minimal : debug, log, warn ou error. Par défaut : log.")] string? level = null,
        [Description("Ne garder que les messages du chargement en cours.")] bool sinceLoad = false,
        [Description("Nombre maximal de messages rendus (les plus récents), de 1 à 500.")] int limit = McpBrowser.DefaultLimit,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Console, () => new { pane, level = McpBrowser.Level(level), sinceLoad, limit = McpBrowser.Limit(limit) }, token);

    private static Task<CallToolResult> NetworkAsync(
        [Description(PaneDescription)] string? pane = null,
        [Description("Ne garder que les requêtes en erreur (statut ≥ 400, échec réseau).")] bool failedOnly = false,
        [Description("Ne garder que les requêtes du chargement en cours.")] bool sinceLoad = false,
        [Description("Nombre maximal de requêtes rendues (les plus récentes), de 1 à 500.")] int limit = McpBrowser.DefaultLimit,
        CancellationToken token = default) =>
        TilyConnection.CallAsync(McpBrowser.Network, new { pane, failedOnly, sinceLoad, limit = McpBrowser.Limit(limit) }, token);

    private static Task<CallToolResult> SnapshotAsync([Description(PaneDescription)] string? pane = null, CancellationToken token = default) =>
        TilyConnection.CallAsync(McpBrowser.Snapshot, new { pane }, token);

    private static Task<CallToolResult> ClickAsync(
        [Description("Référence de l’élément, donnée par tily_browser_snapshot (e12).")] string reference,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Click, () => new { reference = McpBrowser.Ref(reference), pane }, token);

    private static Task<CallToolResult> TypeAsync(
        [Description("Référence du champ, donnée par tily_browser_snapshot (e12).")] string reference,
        [Description("Texte à saisir (4 000 caractères au plus).")] string text,
        [Description(PaneDescription)] string? pane = null,
        [Description("Remplacer le contenu existant du champ.")] bool clear = false,
        [Description("Valider avec Entrée après la saisie.")] bool submit = false,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Type, () => new { reference = McpBrowser.Ref(reference), text = McpBrowser.Text(text), pane, clear, submit }, token);

    private static Task<CallToolResult> PressAsync(
        [Description("Touche ou combinaison : Enter, Tab, Escape, Backspace, ArrowDown, Ctrl+A, Cmd+Enter…")] string key,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Press, () => new { chord = McpBrowser.Chord(key), pane }, token);

    private static Task<CallToolResult> SelectAsync(
        [Description("Référence de la liste déroulante, donnée par tily_browser_snapshot (e12).")] string reference,
        [Description("Valeur ou libellé de l’option.")] string option,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Select, () => new { reference = McpBrowser.Ref(reference), option = McpBrowser.Option(option), pane }, token);

    private static Task<CallToolResult> WaitForAsync(
        [Description("Texte à attendre dans la page.")] string? text = null,
        [Description("Référence d’un élément (dernier snapshot) à attendre visible.")] string? reference = null,
        [Description("Fragment d’adresse à attendre.")] string? url = null,
        [Description("Attendre la disparition au lieu de l’apparition.")] bool gone = false,
        [Description("Délai maximal en millisecondes, de 100 à 30 000.")] int timeoutMs = McpBrowser.DefaultWaitMs,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.WaitFor, () =>
        {
            var condition = McpBrowser.Condition(text, reference, url);
            return new { text = condition.Text, reference = condition.Ref, url = condition.Url, gone, timeoutMs = McpBrowser.WaitMs(timeoutMs), pane };
        }, token);

    private static Task<CallToolResult> EvaluateAsync(
        [Description("Expression JavaScript (20 000 caractères au plus), par exemple document.title.")] string expression,
        [Description(PaneDescription)] string? pane = null,
        CancellationToken token = default) =>
        TilyConnection.CallCheckedAsync(McpBrowser.Evaluate, () => new { expression = McpBrowser.Expression(expression), pane }, token);

    private static async Task<CallToolResult> ScreenshotAsync(
        [Description(PaneDescription)] string? pane = null,
        [Description(ViewportDescription + " Par défaut : la largeur du pane.")] string? viewport = null,
        [Description("Capturer toute la hauteur de la page.")] bool fullPage = false,
        [Description("Fichier .png où enregistrer aussi la capture, relatif à votre dossier.")] string? path = null,
        CancellationToken token = default)
    {
        string? file;
        object arguments;
        try
        {
            file = McpBrowser.ScreenshotPath(path, Environment.CurrentDirectory);
            arguments = new { pane, viewport = McpBrowser.Viewport(viewport), fullPage };
        }
        catch (InvalidOperationException exception)
        {
            return TilyConnection.Failure(exception.Message);
        }

        var response = await TilyConnection.SendAsync(McpBrowser.Screenshot, arguments, token);
        if (response.Error is not null || response.Result is not { } result || JsonNode.Parse(result.GetRawText()) is not JsonObject details || details["png"]?.GetValue<string>() is not { } png)
        {
            return TilyConnection.Failure(response.Error ?? McpPipe.UnreadableAnswer);
        }

        var bytes = Convert.FromBase64String(png);
        details.Remove("png");
        if (file is not null)
        {
            try
            {
                await File.WriteAllBytesAsync(file, bytes, token);
                details["path"] = file;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                details["pathError"] = UserErrorMessage.Of(exception);
            }
        }

        return new CallToolResult
        {
            Content = [ImageContentBlock.FromBytes(bytes, "image/png"), new TextContentBlock { Text = details.ToJsonString(McpPipe.JsonOptions) }]
        };
    }
}
