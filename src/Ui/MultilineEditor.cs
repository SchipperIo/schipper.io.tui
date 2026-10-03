using System.Text;
using Schipper.Io.Ansi.Input;
using Schipper.Io.Ansi.Screen;
using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Themes;

namespace Schipper.Io.Tui.Ui;

/// <summary>
/// A modest full-screen multi-line text editor: arrow/Home/End navigation, insert/split/join editing,
/// and a sticky hint bar. <c>Ctrl-S</c> saves and exits; <c>Ctrl-X</c> or <c>Esc</c> cancels. It runs
/// its own render/input loop against a <see cref="TerminalView"/>, so feature screens (board posts,
/// private mail, bulletins, networked posts) all compose through the same widget.
/// </summary>
public static class MultilineEditor
{
    public const int MaxLines = 200;
    public const int MaxLineLength = 250;

    /// <summary>
    /// Edits text seeded from <paramref name="initial"/>. Returns the joined text on save (Ctrl-S), or
    /// null if the user cancelled (Ctrl-X / Esc). Trailing blank lines are trimmed from the result.
    /// Lines longer than <see cref="MaxLineLength"/> are wrapped on load; joining two lines that would
    /// exceed that limit re-wraps them so the original characters are kept.
    /// </summary>
    public static async Task<string?> EditAsync(
        TerminalView view,
        InputReader reader,
        Theme theme,
        string title,
        string? initial,
        TerminalUi.BackgroundPainter? background = null,
        CancellationToken cancellationToken = default)
    {
        theme = theme.ForDepth(view.Depth);
        List<StringBuilder> lines = SplitInitial(initial);
        int row = lines.Count - 1;
        int col = lines[row].Length;
        int top = 0; // first visible line

        while (true)
        {
            view.SyncSize();
            ScreenBuffer buffer = view.Buffer;
            if (background is not null)
            {
                background(buffer, theme);
            }
            else
            {
                Draw.FillBackground(buffer, theme.Background);
            }

            int boxX = 1;
            int boxW = buffer.Width - 2;
            int boxY = 0;
            int boxH = buffer.Height;
            int textTop = boxY + 1;
            int textRows = Math.Max(1, boxH - 3);
            int textLeft = boxX + 2;
            int textWidth = boxW - 4;

            Draw.FillRect(buffer, boxX, boxY, boxW, boxH, ' ', theme.MenuForeground, theme.MenuBackground);
            Draw.TitledBox(buffer, boxX, boxY, boxW, boxH, title, theme.FrameForeground, theme.TitleForeground, theme.MenuBackground, doubleLine: true);

            // Keep the cursor row within the visible window.
            if (row < top)
            {
                top = row;
            }
            else if (row >= top + textRows)
            {
                top = row - textRows + 1;
            }

            // Horizontal scroll so the cursor column stays visible on long lines.
            int colOffset = textWidth >= 1 && col >= textWidth ? col - textWidth + 1 : 0;

            if (textWidth >= 1)
            {
                for (int i = 0; i < textRows; i++)
                {
                    int lineIndex = top + i;
                    if (lineIndex >= lines.Count)
                    {
                        break;
                    }

                    string text = lines[lineIndex].ToString();
                    string shown = colOffset < text.Length ? text[colOffset..] : string.Empty;
                    if (shown.Length > textWidth)
                    {
                        shown = shown[..textWidth];
                    }

                    buffer.DrawText(textLeft, textTop + i, shown, theme.Foreground, theme.MenuBackground);
                }
            }

            // Cursor block.
            int curScreenY = textTop + (row - top);
            int curScreenX = textLeft + (col - colOffset);
            if (buffer.InBounds(curScreenX, curScreenY) && curScreenY >= textTop && curScreenY < textTop + textRows)
            {
                Cell under = buffer[curScreenX, curScreenY];
                char glyph = under.Glyph == '\0' ? ' ' : under.Glyph;
                buffer.Set(curScreenX, curScreenY, glyph, theme.AccentForeground, theme.MenuSelectedBackground);
            }

            buffer.DrawText(boxX + 2, boxY + boxH - 1,
                $"Ctrl-S save · Ctrl-X cancel · {row + 1}:{col + 1} · {lines.Count} line{(lines.Count == 1 ? string.Empty : "s")}",
                theme.MutedForeground, theme.MenuBackground);

            await view.FlushAsync(cancellationToken).ConfigureAwait(false);

            KeyEvent? maybeKey = await reader.ReadKeyAsync(cancellationToken).ConfigureAwait(false);
            if (maybeKey is not { } key)
            {
                return null; // disconnected
            }

            // --- Commands ---
            if (key.IsChar && key.Ctrl && char.ToLowerInvariant(key.Char) == 's')
            {
                return Finish(lines);
            }

            if (key.Key == Key.Escape || (key.IsChar && key.Ctrl && char.ToLowerInvariant(key.Char) == 'x'))
            {
                return null;
            }

            // --- Navigation & editing ---
            switch (key.Key)
            {
                case Key.Left:
                    if (col > 0)
                    {
                        col--;
                    }
                    else if (row > 0)
                    {
                        row--;
                        col = lines[row].Length;
                    }

                    break;

                case Key.Right:
                    if (col < lines[row].Length)
                    {
                        col++;
                    }
                    else if (row < lines.Count - 1)
                    {
                        row++;
                        col = 0;
                    }

                    break;

                case Key.Up when row > 0:
                    row--;
                    col = Math.Min(col, lines[row].Length);
                    break;

                case Key.Down when row < lines.Count - 1:
                    row++;
                    col = Math.Min(col, lines[row].Length);
                    break;

                case Key.Home:
                    col = 0;
                    break;

                case Key.End:
                    col = lines[row].Length;
                    break;

                case Key.Enter:
                    if (lines.Count < MaxLines)
                    {
                        StringBuilder cur = lines[row];
                        string tail = cur.ToString(col, cur.Length - col);
                        cur.Length = col;
                        lines.Insert(row + 1, new StringBuilder(tail));
                        row++;
                        col = 0;
                    }

                    break;

                case Key.Backspace:
                    if (col > 0)
                    {
                        lines[row].Remove(col - 1, 1);
                        col--;
                    }
                    else if (row > 0)
                    {
                        col = lines[row - 1].Length;
                        JoinNextLine(lines, row - 1);
                        row--;
                    }

                    break;

                case Key.Delete:
                    if (col < lines[row].Length)
                    {
                        lines[row].Remove(col, 1);
                    }
                    else if (row < lines.Count - 1)
                    {
                        JoinNextLine(lines, row);
                    }

                    break;

                case Key.Char when !key.Ctrl && key.Char >= ' ' && lines[row].Length < MaxLineLength:
                    lines[row].Insert(col, key.Char);
                    col++;
                    break;
            }
        }
    }

    private static List<StringBuilder> SplitInitial(string? initial)
    {
        List<StringBuilder> lines = [];
        foreach (string line in (initial ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
        {
            AppendInitialLine(lines, line);
        }

        if (lines.Count == 0)
        {
            lines.Add(new StringBuilder());
        }

        return lines;
    }

    private static void AppendInitialLine(List<StringBuilder> lines, string line)
    {
        if (line.Length == 0)
        {
            if (lines.Count < MaxLines)
            {
                lines.Add(new StringBuilder());
            }

            return;
        }

        int offset = 0;
        while (offset < line.Length)
        {
            int remaining = line.Length - offset;
            bool lastSlot = lines.Count + 1 >= MaxLines;
            if (lastSlot)
            {
                if (lines.Count < MaxLines)
                {
                    lines.Add(new StringBuilder(line[offset..]));
                }
                else
                {
                    lines[^1].Append(line[offset..]);
                }

                return;
            }

            int take = Math.Min(MaxLineLength, remaining);
            lines.Add(new StringBuilder(line, offset, take, take));
            offset += take;
        }
    }

    private static void JoinNextLine(List<StringBuilder> lines, int row)
    {
        StringBuilder head = lines[row];
        StringBuilder tail = lines[row + 1];
        int combined = head.Length + tail.Length;
        if (combined <= MaxLineLength)
        {
            head.Append(tail);
            lines.RemoveAt(row + 1);
            return;
        }

        string merged = string.Concat(head, tail);
        head.Clear();
        head.Append(merged.AsSpan(0, MaxLineLength));
        tail.Clear();
        tail.Append(merged.AsSpan(MaxLineLength));
    }

    private static string Finish(List<StringBuilder> lines)
    {
        List<string> result = [.. lines.Select(l => l.ToString())];
        while (result.Count > 0 && result[^1].Length == 0)
        {
            result.RemoveAt(result.Count - 1);
        }

        return string.Join("\n", result);
    }
}
