namespace Schipper.Io.Tui.Ui;

/// <summary>A single selectable entry in a <see cref="TerminalUi"/> menu.</summary>
public sealed class MenuItem
{
    public MenuItem(string label, char hotkey = '\0', object? tag = null, bool enabled = true)
    {
        Label = label;
        Hotkey = hotkey == '\0' ? '\0' : char.ToLowerInvariant(hotkey);
        Tag = tag;
        Enabled = enabled;
    }

    /// <summary>Display text.</summary>
    public string Label { get; }

    /// <summary>Optional shortcut key (case-insensitive); '\0' for none.</summary>
    public char Hotkey { get; }

    /// <summary>Caller-defined payload (e.g., an action id).</summary>
    public object? Tag { get; }

    public bool Enabled { get; }

    /// <summary>A non-selectable separator row.</summary>
    public static MenuItem Separator { get; } = new(string.Empty, enabled: false);

    public bool IsSeparator => Label.Length == 0 && !Enabled;
}
