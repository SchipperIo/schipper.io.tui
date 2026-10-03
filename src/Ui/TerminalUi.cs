using System.Text;
using Schipper.Io.Ansi.Art;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Input;
using Schipper.Io.Ansi.Screen;
using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Themes;

namespace Schipper.Io.Tui.Ui;

/// <summary>
/// High-level, async interactive primitives for building BBS-style modal flows: scrollable menus,
/// line input, confirmation, and message boxes. Each method runs its own render/input loop against a
/// <see cref="TerminalView"/> and <see cref="InputReader"/>, returning when the user makes a choice.
/// </summary>
public static class TerminalUi
{
    /// <summary>Optional painter that fills the background before a widget is drawn over it.</summary>
    public delegate void BackgroundPainter(ScreenBuffer buffer, Theme theme);

    /// <summary>
    /// Runs a vertical menu and returns the chosen item index, or -1 if the user pressed Escape or
    /// disconnected. <paramref name="background"/> paints behind the menu (e.g., the welcome art).
    /// </summary>
    public static async ValueTask<int> MenuAsync(
        TerminalView view,
        InputReader reader,
        Theme theme,
        string title,
        IReadOnlyList<MenuItem> items,
        BackgroundPainter? background = null,
        int? anchorTop = null,
        CancellationToken cancellationToken = default)
    {
        theme = theme.ForDepth(view.Depth);
        if (items.Count == 0)
        {
            return -1;
        }

        int selected = FirstSelectable(items, 0, 1);
        int top = 0;

        while (true)
        {
            view.SyncSize();
            ScreenBuffer buffer = view.Buffer;
            PaintBackground(buffer, theme, background);

            int innerWidth = MenuWidth(title, items);
            int boxWidth = Math.Min(buffer.Width, innerWidth + 6);
            int desiredHeight = items.Count + 2;
            int boxHeight = Math.Min(desiredHeight, buffer.Height);
            int boxX = Math.Max(0, (buffer.Width - boxWidth) / 2);
            int boxY = anchorTop ?? Math.Max(0, (buffer.Height - boxHeight) / 2);
            boxY = Math.Clamp(boxY, 0, Math.Max(0, buffer.Height - boxHeight));

            Draw.FillRect(buffer, boxX, boxY, boxWidth, boxHeight, ' ', theme.MenuForeground, theme.MenuBackground);
            Draw.TitledBox(buffer, boxX, boxY, boxWidth, boxHeight, title, theme.FrameForeground, theme.TitleForeground, theme.MenuBackground, doubleLine: true);

            int visibleRows = Math.Max(0, boxHeight - 2);
            if (visibleRows > 0)
            {
                if (selected < top)
                {
                    top = selected;
                }
                else if (selected >= top + visibleRows)
                {
                    top = selected - visibleRows + 1;
                }

                top = Math.Clamp(top, 0, Math.Max(0, items.Count - visibleRows));
            }

            for (int i = 0; i < visibleRows; i++)
            {
                int itemIndex = top + i;
                if (itemIndex >= items.Count)
                {
                    break;
                }

                MenuItem item = items[itemIndex];
                int rowY = boxY + 1 + i;
                if (item.IsSeparator)
                {
                    buffer.DrawHorizontalLine(boxX + 1, rowY, boxWidth - 2, Glyphs.Horizontal, theme.FrameForeground, theme.MenuBackground);
                    continue;
                }

                bool isSel = itemIndex == selected;
                Color fg = isSel ? theme.MenuSelectedForeground : (item.Enabled ? theme.MenuForeground : theme.MutedForeground);
                Color bg = isSel ? theme.MenuSelectedBackground : theme.MenuBackground;

                Draw.FillRect(buffer, boxX + 1, rowY, boxWidth - 2, 1, ' ', fg, bg);
                // A marker keeps the active row obvious even on clients that ignore background color.
                buffer.Set(boxX + 2, rowY, isSel ? '►' : ' ', isSel ? theme.HotkeyForeground : fg, bg, isSel ? CellAttributes.Bold : CellAttributes.None);
                DrawLabelWithHotkey(buffer, boxX + 4, rowY, item, fg, bg, theme.HotkeyForeground, isSel);
            }

            await view.FlushAsync(cancellationToken).ConfigureAwait(false);

            KeyEvent? maybeKey = await reader.ReadKeyAsync(cancellationToken).ConfigureAwait(false);
            if (maybeKey is not { } key)
            {
                return -1;
            }

            switch (key.Key)
            {
                case Key.Up:
                    selected = Step(items, selected, -1);
                    break;
                case Key.Down:
                    selected = Step(items, selected, +1);
                    break;
                case Key.Enter:
                    if (selected >= 0 && selected < items.Count && items[selected].Enabled)
                    {
                        return selected;
                    }

                    break;
                case Key.Escape:
                    return -1;
                case Key.Char:
                    int hit = FindHotkey(items, key.Char);
                    if (hit >= 0)
                    {
                        return hit;
                    }

                    break;
            }
        }
    }

    /// <summary>Prompts for a line of text with basic editing. Returns null if the user pressed Escape.</summary>
    public static async ValueTask<string?> ReadLineAsync(
        TerminalView view,
        InputReader reader,
        Theme theme,
        string prompt,
        bool mask = false,
        int maxLength = 64,
        BackgroundPainter? background = null,
        CancellationToken cancellationToken = default)
    {
        theme = theme.ForDepth(view.Depth);
        StringBuilder text = new();

        while (true)
        {
            view.SyncSize();
            ScreenBuffer buffer = view.Buffer;
            PaintBackground(buffer, theme, background);

            int boxWidth = Math.Min(buffer.Width - 4, Math.Max(prompt.Length + 4, maxLength + 6));
            int boxHeight = 5;
            int boxX = Math.Max(0, (buffer.Width - boxWidth) / 2);
            int boxY = Math.Max(0, (buffer.Height - boxHeight) / 2);

            Draw.FillRect(buffer, boxX, boxY, boxWidth, boxHeight, ' ', theme.MenuForeground, theme.MenuBackground);
            Draw.Box(buffer, boxX, boxY, boxWidth, boxHeight, theme.FrameForeground, theme.MenuBackground, doubleLine: true);
            buffer.DrawText(boxX + 2, boxY + 1, prompt, theme.TitleForeground, theme.MenuBackground, CellAttributes.Bold);

            int fieldWidth = boxWidth - 4;
            if (fieldWidth >= 1)
            {
                int visibleWidth = Math.Max(0, fieldWidth - 1);
                string shown = mask ? new string('*', text.Length) : text.ToString();
                if (visibleWidth == 0)
                {
                    shown = string.Empty;
                }
                else if (shown.Length > visibleWidth)
                {
                    shown = shown[^visibleWidth..];
                }

                Draw.FillRect(buffer, boxX + 2, boxY + 2, fieldWidth, 1, ' ', theme.InputForeground, theme.InputBackground);
                buffer.DrawText(boxX + 2, boxY + 2, shown, theme.InputForeground, theme.InputBackground);
                int cursorX = boxX + 2 + shown.Length;
                buffer.Set(cursorX, boxY + 2, ' ', theme.InputForeground, theme.MenuSelectedBackground);
            }

            await view.FlushAsync(cancellationToken).ConfigureAwait(false);

            KeyEvent? maybeKey = await reader.ReadKeyAsync(cancellationToken).ConfigureAwait(false);
            if (maybeKey is not { } key)
            {
                return null;
            }

            switch (key.Key)
            {
                case Key.Enter:
                    return text.ToString();
                case Key.Escape:
                    return null;
                case Key.Backspace:
                    if (text.Length > 0)
                    {
                        text.Length--;
                    }

                    break;
                case Key.Char when !key.Ctrl && key.Char >= ' ' && text.Length < maxLength:
                    text.Append(key.Char);
                    break;
            }
        }
    }

    /// <summary>Shows a message and waits for any key.</summary>
    public static async ValueTask MessageAsync(
        TerminalView view,
        InputReader reader,
        Theme theme,
        string title,
        string message,
        Color? messageColor = null,
        BackgroundPainter? background = null,
        CancellationToken cancellationToken = default)
    {
        theme = theme.ForDepth(view.Depth);
        view.SyncSize();
        ScreenBuffer buffer = view.Buffer;
        PaintBackground(buffer, theme, background);

        int boxWidth = Math.Min(buffer.Width - 4, Math.Max(title.Length, message.Length) + 6);
        int boxHeight = 6;
        int boxX = Math.Max(0, (buffer.Width - boxWidth) / 2);
        int boxY = Math.Max(0, (buffer.Height - boxHeight) / 2);

        Draw.FillRect(buffer, boxX, boxY, boxWidth, boxHeight, ' ', theme.MenuForeground, theme.MenuBackground);
        Draw.TitledBox(buffer, boxX, boxY, boxWidth, boxHeight, title, theme.FrameForeground, theme.TitleForeground, theme.MenuBackground, doubleLine: true);
        Draw.CenteredTextIn(buffer, boxX + 1, boxY + 2, boxWidth - 2, message, messageColor ?? theme.MenuForeground, theme.MenuBackground);
        Draw.CenteredTextIn(buffer, boxX + 1, boxY + boxHeight - 2, boxWidth - 2, "Press any key", theme.FrameForeground, theme.MenuBackground);

        await view.FlushAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadKeyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Asks a yes/no question; returns true for yes.</summary>
    public static async ValueTask<bool> ConfirmAsync(
        TerminalView view,
        InputReader reader,
        Theme theme,
        string question,
        BackgroundPainter? background = null,
        CancellationToken cancellationToken = default)
    {
        theme = theme.ForDepth(view.Depth);
        MenuItem[] items =
        [
            new MenuItem("Yes", 'y', tag: true),
            new MenuItem("No", 'n', tag: false),
        ];

        int choice = await MenuAsync(view, reader, theme, question, items, background, cancellationToken: cancellationToken).ConfigureAwait(false);
        return choice >= 0 && items[choice].Tag is true;
    }

    private static void PaintBackground(ScreenBuffer buffer, Theme theme, BackgroundPainter? background)
    {
        if (background is not null)
        {
            background(buffer, theme);
        }
        else
        {
            Draw.FillBackground(buffer, theme.Background);
        }
    }

    /// <summary>Draws a menu label, underlining and recoloring the first occurrence of its hotkey.</summary>
    private static void DrawLabelWithHotkey(ScreenBuffer buffer, int x, int y, MenuItem item, Color fg, Color bg, Color keyFg, bool selected)
    {
        CellAttributes attr = selected ? CellAttributes.Bold : CellAttributes.None;
        buffer.DrawText(x, y, item.Label, fg, bg, attr);

        if (item.Hotkey == '\0')
        {
            return;
        }

        int idx = item.Label.ToLowerInvariant().IndexOf(item.Hotkey);
        if (idx >= 0)
        {
            buffer.Set(x + idx, y, item.Label[idx], keyFg, bg, CellAttributes.Bold | CellAttributes.Underline);
        }
    }

    private static int MenuWidth(string title, IReadOnlyList<MenuItem> items)
    {
        int width = title.Length;
        foreach (MenuItem item in items)
        {
            width = Math.Max(width, item.Label.Length);
        }

        return Math.Max(width, 12);
    }

    private static int Step(IReadOnlyList<MenuItem> items, int current, int direction)
    {
        for (int i = 0; i < items.Count; i++)
        {
            current = (current + direction + items.Count) % items.Count;
            if (items[current].Enabled)
            {
                return current;
            }
        }

        return current;
    }

    private static int FirstSelectable(IReadOnlyList<MenuItem> items, int start, int direction)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int idx = (start + (i * direction) + items.Count) % items.Count;
            if (items[idx].Enabled)
            {
                return idx;
            }
        }

        return 0;
    }

    private static int FindHotkey(IReadOnlyList<MenuItem> items, char c)
    {
        char lower = char.ToLowerInvariant(c);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Enabled && items[i].Hotkey == lower)
            {
                return i;
            }
        }

        return -1;
    }
}
