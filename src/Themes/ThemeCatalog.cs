using Schipper.Io.Ansi.Colors;

namespace Schipper.Io.Tui.Themes;

/// <summary>
/// The set of named color schemes a user can choose from, Renegade's per-user "color scheme" idea.
/// Each theme is authored as a truecolor palette with a hand-tuned 16-color
/// <see cref="Theme.Fallback16"/> (the theme's original basic-color incarnation, so 16-color
/// clients see exactly what they always did). All are resolvable by name; unknown names fall back
/// to the Seashell <see cref="Theme.Default"/>.
/// </summary>
public static class ThemeCatalog
{
    /// <summary>The 16-color Seashell: the original cyan-on-black default scheme.</summary>
    private static readonly Theme SeashellFallback = new() { Name = "Seashell" };

    /// <summary>
    /// Bioluminescent tide pools: deep ocean-navy backgrounds, cyan→teal accents, warm sand
    /// hotkeys, coral errors, and soft foam-white text.
    /// </summary>
    public static Theme Seashell { get; } = new()
    {
        Name = "Seashell",
        Fallback16 = SeashellFallback,

        Background = Color.Rgb(8, 20, 34),               // abyssal navy
        Foreground = Color.Rgb(222, 238, 240),           // sea-foam white

        FrameForeground = Color.Rgb(40, 150, 160),       // weathered teal
        FrameBackground = Color.Rgb(11, 26, 41),         // panel navy
        TitleForeground = Color.Rgb(96, 226, 230),       // bright aqua

        MenuForeground = Color.Rgb(133, 200, 208),       // shallow-water cyan
        MenuBackground = Color.Rgb(11, 26, 41),          // panel navy
        MenuSelectedForeground = Color.Rgb(240, 251, 252), // foam crest
        MenuSelectedBackground = Color.Rgb(16, 78, 96),  // lagoon teal
        HotkeyForeground = Color.Rgb(245, 200, 122),     // warm sand

        MutedForeground = Color.Rgb(92, 122, 138),       // slate mist

        HeaderForeground = Color.Rgb(232, 246, 247),     // foam white
        HeaderBackground = Color.Rgb(12, 58, 74),        // deep-teal bar
        HeaderAccent = Color.Rgb(126, 240, 234),         // bioluminescent aqua

        FooterForeground = Color.Rgb(208, 232, 236),     // pale foam
        FooterBackground = Color.Rgb(12, 58, 74),        // deep-teal bar
        FooterKeyForeground = Color.Rgb(245, 200, 122),  // warm sand
        FooterSeparator = Color.Rgb(96, 226, 230),       // bright aqua

        StatusForeground = Color.Rgb(6, 16, 26),         // ink navy
        StatusBackground = Color.Rgb(102, 212, 200),     // seafoam

        AccentForeground = Color.Rgb(255, 244, 232),     // sunlit shell
        ErrorForeground = Color.Rgb(255, 124, 108),      // coral
        SuccessForeground = Color.Rgb(118, 228, 172),    // sea-glass green

        InputForeground = Color.Rgb(240, 250, 251),      // foam white
        InputBackground = Color.Rgb(15, 52, 66),         // tide-pool teal
    };

    /// <summary>The 16-color Amber this catalog shipped before truecolor.</summary>
    private static readonly Theme AmberFallback = new()
    {
        Name = "Amber",
        Foreground = Color.Basic(BasicColor.BrightYellow),
        FrameForeground = Color.Basic(BasicColor.Yellow),
        TitleForeground = Color.Basic(BasicColor.BrightYellow),
        MenuForeground = Color.Basic(BasicColor.Yellow),
        MenuSelectedForeground = Color.Basic(BasicColor.Black),
        MenuSelectedBackground = Color.Basic(BasicColor.Yellow),
        HotkeyForeground = Color.Basic(BasicColor.BrightWhite),
        MutedForeground = Color.Basic(BasicColor.BrightBlack),
        HeaderForeground = Color.Basic(BasicColor.Black),
        HeaderBackground = Color.Basic(BasicColor.Yellow),
        HeaderAccent = Color.Basic(BasicColor.Black),
        FooterForeground = Color.Basic(BasicColor.Black),
        FooterBackground = Color.Basic(BasicColor.Yellow),
        FooterKeyForeground = Color.Basic(BasicColor.BrightWhite),
        FooterSeparator = Color.Basic(BasicColor.BrightYellow),
        StatusBackground = Color.Basic(BasicColor.Yellow),
        AccentForeground = Color.Basic(BasicColor.BrightYellow),
        InputForeground = Color.Basic(BasicColor.BrightYellow),
    };

    /// <summary>
    /// A Wyse/IBM 3151 amber CRT: warm black glass, a monochrome amber phosphor ramp in subtle
    /// brightness tiers (dim trim → body copy → bright emphasis), and a single red accent reserved
    /// for errors.
    /// </summary>
    public static Theme Amber { get; } = new()
    {
        Name = "Amber",
        Fallback16 = AmberFallback,

        Background = Color.Rgb(16, 10, 4),               // warm black glass
        Foreground = Color.Rgb(255, 176, 0),             // classic amber phosphor

        FrameForeground = Color.Rgb(198, 130, 8),        // dimmed phosphor trim
        FrameBackground = Color.Rgb(22, 14, 6),          // panel glass
        TitleForeground = Color.Rgb(255, 202, 64),       // bright tier

        MenuForeground = Color.Rgb(232, 156, 16),        // body tier
        MenuBackground = Color.Rgb(22, 14, 6),           // panel glass
        MenuSelectedForeground = Color.Rgb(24, 14, 2),   // etched glass
        MenuSelectedBackground = Color.Rgb(255, 176, 0), // phosphor bar
        HotkeyForeground = Color.Rgb(255, 232, 170),     // hottest tier

        MutedForeground = Color.Rgb(138, 90, 22),        // faded burn-in

        HeaderForeground = Color.Rgb(26, 16, 4),         // etched glass
        HeaderBackground = Color.Rgb(224, 148, 0),       // phosphor bar
        HeaderAccent = Color.Rgb(66, 38, 4),             // deep burn

        FooterForeground = Color.Rgb(26, 16, 4),         // etched glass
        FooterBackground = Color.Rgb(224, 148, 0),       // phosphor bar
        FooterKeyForeground = Color.Rgb(255, 244, 214),  // white-hot key caps
        FooterSeparator = Color.Rgb(255, 202, 64),       // bright tier

        StatusForeground = Color.Rgb(16, 10, 4),         // warm black
        StatusBackground = Color.Rgb(255, 176, 0),       // phosphor bar

        AccentForeground = Color.Rgb(255, 214, 96),      // bright tier
        ErrorForeground = Color.Rgb(255, 64, 48),        // the single red accent
        SuccessForeground = Color.Rgb(255, 232, 170),    // stays in the amber ramp

        InputForeground = Color.Rgb(255, 214, 96),       // bright tier
        InputBackground = Color.Rgb(52, 32, 8),          // recessed amber well
    };

    /// <summary>The 16-color Matrix this catalog shipped before truecolor.</summary>
    private static readonly Theme MatrixFallback = new()
    {
        Name = "Matrix",
        Foreground = Color.Basic(BasicColor.BrightGreen),
        FrameForeground = Color.Basic(BasicColor.Green),
        TitleForeground = Color.Basic(BasicColor.BrightGreen),
        MenuForeground = Color.Basic(BasicColor.Green),
        MenuSelectedForeground = Color.Basic(BasicColor.Black),
        MenuSelectedBackground = Color.Basic(BasicColor.Green),
        HotkeyForeground = Color.Basic(BasicColor.BrightWhite),
        MutedForeground = Color.Basic(BasicColor.BrightBlack),
        HeaderForeground = Color.Basic(BasicColor.Black),
        HeaderBackground = Color.Basic(BasicColor.Green),
        HeaderAccent = Color.Basic(BasicColor.Black),
        FooterForeground = Color.Basic(BasicColor.Black),
        FooterBackground = Color.Basic(BasicColor.Green),
        FooterKeyForeground = Color.Basic(BasicColor.BrightWhite),
        FooterSeparator = Color.Basic(BasicColor.BrightGreen),
        StatusBackground = Color.Basic(BasicColor.Green),
        AccentForeground = Color.Basic(BasicColor.BrightGreen),
        InputForeground = Color.Basic(BasicColor.BrightGreen),
    };

    /// <summary>
    /// Layered green phosphor on near-black: dim structural greens, a mid body green, a bright
    /// highlight, and a pale mint reserved for emphasis. Restrained — not neon soup.
    /// </summary>
    public static Theme Matrix { get; } = new()
    {
        Name = "Matrix",
        Fallback16 = MatrixFallback,

        Background = Color.Rgb(3, 9, 5),                 // powered-off glass
        Foreground = Color.Rgb(80, 200, 110),            // mid phosphor

        FrameForeground = Color.Rgb(30, 110, 58),        // dim structural green
        FrameBackground = Color.Rgb(6, 16, 9),           // panel black
        TitleForeground = Color.Rgb(110, 240, 140),      // bright phosphor

        MenuForeground = Color.Rgb(62, 168, 92),         // body green
        MenuBackground = Color.Rgb(6, 16, 9),            // panel black
        MenuSelectedForeground = Color.Rgb(4, 12, 6),    // inverse glass
        MenuSelectedBackground = Color.Rgb(62, 190, 96), // lit phosphor bar
        HotkeyForeground = Color.Rgb(200, 255, 220),     // pale mint emphasis

        MutedForeground = Color.Rgb(38, 78, 50),         // afterglow

        HeaderForeground = Color.Rgb(4, 12, 6),          // inverse glass
        HeaderBackground = Color.Rgb(38, 148, 70),       // lit bar
        HeaderAccent = Color.Rgb(8, 42, 20),             // deep green shadow

        FooterForeground = Color.Rgb(4, 12, 6),          // inverse glass
        FooterBackground = Color.Rgb(38, 148, 70),       // lit bar
        FooterKeyForeground = Color.Rgb(214, 255, 228),  // mint key caps
        FooterSeparator = Color.Rgb(110, 240, 140),      // bright phosphor

        StatusForeground = Color.Rgb(3, 9, 5),           // glass black
        StatusBackground = Color.Rgb(62, 190, 96),       // lit phosphor bar

        AccentForeground = Color.Rgb(200, 255, 220),     // pale mint emphasis
        ErrorForeground = Color.Rgb(255, 96, 80),        // alarm red
        SuccessForeground = Color.Rgb(110, 240, 140),    // bright phosphor

        InputForeground = Color.Rgb(184, 255, 204),      // mint on well
        InputBackground = Color.Rgb(10, 34, 18),         // recessed green well
    };

    /// <summary>Every selectable theme, default first.</summary>
    public static IReadOnlyList<Theme> All { get; } = [Seashell, Amber, Matrix];

    /// <summary>The selectable theme names, for menus.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. All.Select(t => t.Name)];

    /// <summary>Returns the theme with the given name (case-insensitive), or the default if unknown/blank.</summary>
    public static Theme Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Theme.Default;
        }

        foreach (Theme theme in All)
        {
            if (string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return theme;
            }
        }

        return Theme.Default;
    }
}
