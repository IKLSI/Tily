namespace Tily.Core.Appearance;

public sealed record TerminalThemeModel(string Background, string Foreground, string Cursor, string SelectionBackground);

public sealed record AccentSchemeModel(IReadOnlyDictionary<string, string> Tokens, TerminalThemeModel Terminal);

public sealed record AccentThemeModel(string Accent, IReadOnlyDictionary<string, string> Tokens, TerminalThemeModel Terminal, AccentSchemeModel Light);

public static class AccentTheme
{
    public const string AccentToken = "green";
    private const double MinAccentLightness = 0.55;
    private const double MaxAccentLightness = 0.8;
    private const double MaxAccentChroma = 0.25;
    private const double MaxSurfaceChroma = 0.07;
    private const double MaxToneChroma = 0.16;
    private static readonly HashSet<string> ToneTokens = ["green-deep", "focus"];
    private const double MinLightAccentLightness = 0.5;
    private const double MaxLightAccentLightness = 0.68;
    private const string SelectionAlpha = "40";
    private const string LightSelectionAlpha = "33";

    private static readonly (string Token, double Lightness, double ChromaRatio)[] Shades =
    [
        ("paper", 0.200, 0.115),
        ("panel", 0.240, 0.13),
        ("raised", 0.282, 0.15),
        ("line", 0.335, 0.165),
        ("line-soft", 0.272, 0.14),
        ("ink", 0.8845, 0.105),
        ("ink-soft", 0.723, 0.175),
        ("muted", 0.675, 0.205),
        ("green-deep", 0.804, 0.68),
        ("green-soft", 0.286, 0.235),
        ("green-hover", 0.315, 0.16),
        ("focus", 0.737, 0.88),
        ("terminal", 0.178, 0.105),
        ("terminal-ink", 0.853, 0.125),
        ("scrollbar", 0.3735, 0.2),
        ("scrollbar-hover", 0.468, 0.24)
    ];

    private static readonly (string Token, double Lightness, double ChromaRatio)[] LightShades =
    [
        ("paper", 0.985, 0.05),
        ("panel", 0.96, 0.06),
        ("raised", 0.995, 0.03),
        ("line", 0.87, 0.09),
        ("line-soft", 0.925, 0.07),
        ("ink", 0.24, 0.06),
        ("ink-soft", 0.42, 0.08),
        ("muted", 0.55, 0.09),
        ("green-deep", 0.46, 0.68),
        ("green-soft", 0.92, 0.35),
        ("green-hover", 0.935, 0.2),
        ("focus", 0.6, 0.88),
        ("terminal", 0.99, 0.04),
        ("terminal-ink", 0.27, 0.06),
        ("scrollbar", 0.78, 0.1),
        ("scrollbar-hover", 0.66, 0.12)
    ];

    public static AccentThemeModel Build(string accent)
    {
        var normalized = HexColor.Normalize(accent) ?? throw new ArgumentException($"Couleur invalide : {accent}.", nameof(accent));
        var source = OklchColor.FromHex(normalized);
        var lightness = Math.Clamp(source.Lightness, MinAccentLightness, MaxAccentLightness);
        var chroma = Math.Min(source.Chroma, MaxAccentChroma);
        var accentHex = lightness == source.Lightness && chroma == source.Chroma ? normalized : new OklchColor(lightness, chroma, source.Hue).ToHex();
        var tokens = Shades.ToDictionary(shade => shade.Token, shade => new OklchColor(shade.Lightness, ShadeChroma(shade.Token, chroma) * shade.ChromaRatio, source.Hue).ToHex());
        tokens[AccentToken] = accentHex;
        var terminal = new TerminalThemeModel(tokens["terminal"], tokens["terminal-ink"], tokens["terminal-ink"], accentHex + SelectionAlpha);
        return new AccentThemeModel(accentHex, tokens, terminal, BuildLight(source));
    }

    private static double ShadeChroma(string token, double chroma) => Math.Min(chroma, ToneTokens.Contains(token) ? MaxToneChroma : MaxSurfaceChroma);

    private static AccentSchemeModel BuildLight(OklchColor source)
    {
        var lightness = Math.Clamp(source.Lightness, MinLightAccentLightness, MaxLightAccentLightness);
        var accentChroma = Math.Min(source.Chroma, MaxAccentChroma);
        var accentHex = new OklchColor(lightness, accentChroma, source.Hue).ToHex();
        var tokens = LightShades.ToDictionary(shade => shade.Token, shade => new OklchColor(shade.Lightness, ShadeChroma(shade.Token, accentChroma) * shade.ChromaRatio, source.Hue).ToHex());
        tokens[AccentToken] = accentHex;
        var terminal = new TerminalThemeModel(tokens["terminal"], tokens["terminal-ink"], tokens["terminal-ink"], accentHex + LightSelectionAlpha);
        return new AccentSchemeModel(tokens, terminal);
    }
}
