using Schipper.Io.Ansi.Colors;

namespace Schipper.Io.Tui.Themes;

/// <summary>
/// A named set of colors driving the look of TUI widgets. Sysops can customize a BBS by swapping
/// the active theme. Slots default to a 16-color cyan-on-black scheme; the catalog themes override
/// every slot with truecolor values and carry a hand-tuned <see cref="Fallback16"/> for clients
/// pinned to <see cref="ColorDepth.Basic16"/>. <see cref="Default"/> is the truecolor Seashell.
/// </summary>
public sealed class Theme
{
    private readonly Theme? _fallback16;

    public required string Name { get; init; }

    public Color Background { get; init; } = Color.Basic(BasicColor.Black);
    public Color Foreground { get; init; } = Color.Basic(BasicColor.White);

    public Color FrameForeground { get; init; } = Color.Basic(BasicColor.Cyan);
    public Color FrameBackground { get; init; } = Color.Basic(BasicColor.Black);
    public Color TitleForeground { get; init; } = Color.Basic(BasicColor.BrightCyan);

    public Color MenuForeground { get; init; } = Color.Basic(BasicColor.Cyan);
    public Color MenuBackground { get; init; } = Color.Basic(BasicColor.Black);
    public Color MenuSelectedForeground { get; init; } = Color.Basic(BasicColor.BrightWhite);
    public Color MenuSelectedBackground { get; init; } = Color.Basic(BasicColor.Blue);
    public Color HotkeyForeground { get; init; } = Color.Basic(BasicColor.BrightYellow);

    /// <summary>Dim text for separators, hints, and inactive detail.</summary>
    public Color MutedForeground { get; init; } = Color.Basic(BasicColor.BrightBlack);

    // Header bar (top of every screen).
    public Color HeaderForeground { get; init; } = Color.Basic(BasicColor.BrightWhite);
    public Color HeaderBackground { get; init; } = Color.Basic(BasicColor.Blue);
    public Color HeaderAccent { get; init; } = Color.Basic(BasicColor.BrightCyan);

    // Footer / sticky hotkey bar (bottom of every screen).
    public Color FooterForeground { get; init; } = Color.Basic(BasicColor.BrightWhite);
    public Color FooterBackground { get; init; } = Color.Basic(BasicColor.Blue);
    public Color FooterKeyForeground { get; init; } = Color.Basic(BasicColor.BrightYellow);
    public Color FooterSeparator { get; init; } = Color.Basic(BasicColor.BrightCyan);

    public Color StatusForeground { get; init; } = Color.Basic(BasicColor.Black);
    public Color StatusBackground { get; init; } = Color.Basic(BasicColor.Cyan);

    public Color AccentForeground { get; init; } = Color.Basic(BasicColor.BrightWhite);
    public Color ErrorForeground { get; init; } = Color.Basic(BasicColor.BrightRed);
    public Color SuccessForeground { get; init; } = Color.Basic(BasicColor.BrightGreen);

    // Text entry fields (line input, editor). Defaults match the historic bright-white-on-blue.
    public Color InputForeground { get; init; } = Color.Basic(BasicColor.BrightWhite);
    public Color InputBackground { get; init; } = Color.Basic(BasicColor.Blue);

    /// <summary>
    /// The hand-tuned 16-color variant of this theme, shown to clients pinned to
    /// <see cref="ColorDepth.Basic16"/> instead of downgrading the truecolor palette
    /// algorithmically. Defaults to the theme itself, so a theme authored purely in basic colors
    /// needs no explicit fallback.
    /// </summary>
    public Theme Fallback16
    {
        get => _fallback16 ?? this;
        init => _fallback16 = value;
    }

    /// <summary>
    /// Returns the variant of this theme to use at the given color depth: the hand-tuned
    /// <see cref="Fallback16"/> for <see cref="ColorDepth.Basic16"/>, otherwise this theme
    /// (256-color clients get the truecolor palette downgraded algorithmically at render time by
    /// the renderer's depth parameter, so they need no hand-tuning).
    /// </summary>
    public Theme ForDepth(ColorDepth depth) => depth == ColorDepth.Basic16 ? Fallback16 : this;

    /// <summary>The default theme: the truecolor Seashell.</summary>
    public static Theme Default => ThemeCatalog.Seashell;
}
