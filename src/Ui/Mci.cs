using System.Text;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;

namespace Schipper.Io.Tui.Ui;

/// <summary>
/// Renders Renegade/Mystic-style "pipe codes" in stored text: a <c>|</c> followed by two digits sets
/// a color — <c>|00</c>–<c>|15</c> the foreground, <c>|16</c>–<c>|23</c> the background — using the
/// classic DOS color order (so <c>|04</c> is red, <c>|14</c> yellow). A lone <c>|</c> or a non-numeric
/// pair is emitted literally. This lets posts, one-liners, bulletins, and the login news carry color.
/// </summary>
public static class Mci
{
    /// <summary>The 16 DOS/CGA colors in pipe-code order, mapped onto Shelly's ANSI palette.</summary>
    private static readonly BasicColor[] DosOrder =
    [
        BasicColor.Black, BasicColor.Blue, BasicColor.Green, BasicColor.Cyan,
        BasicColor.Red, BasicColor.Magenta, BasicColor.Yellow, BasicColor.White,
        BasicColor.BrightBlack, BasicColor.BrightBlue, BasicColor.BrightGreen, BasicColor.BrightCyan,
        BasicColor.BrightRed, BasicColor.BrightMagenta, BasicColor.BrightYellow, BasicColor.BrightWhite,
    ];

    /// <summary>A maximal stretch of text sharing one foreground/background color.</summary>
    public readonly record struct Run(string Text, Color Fg, Color Bg);

    /// <summary>Splits a single line into colored runs, starting from the given default colors.</summary>
    public static IReadOnlyList<Run> ParseLine(string line, Color defaultFg, Color defaultBg)
    {
        var runs = new List<Run>();
        var sb = new StringBuilder();
        Color fg = defaultFg;
        Color bg = defaultBg;

        void Flush()
        {
            if (sb.Length > 0)
            {
                runs.Add(new Run(sb.ToString(), fg, bg));
                sb.Clear();
            }
        }

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '|' && i + 2 < line.Length && TryCode(line[i + 1], line[i + 2], out int code))
            {
                Flush();
                if (code <= 15)
                {
                    fg = Color.Basic(DosOrder[code]);
                }
                else
                {
                    bg = Color.Basic(DosOrder[code - 16]);
                }

                i += 2;
            }
            else
            {
                sb.Append(line[i]);
            }
        }

        Flush();
        return runs;
    }

    /// <summary>Draws one MCI line into the buffer at (x,y), clipping at <paramref name="maxWidth"/>.</summary>
    public static void RenderLine(ScreenBuffer buffer, int x, int y, int maxWidth, string line, Color defaultFg, Color defaultBg)
    {
        int col = 0;
        foreach (Run run in ParseLine(line, defaultFg, defaultBg))
        {
            if (col >= maxWidth)
            {
                break;
            }

            string text = run.Text;
            if (col + text.Length > maxWidth)
            {
                text = text[..(maxWidth - col)];
            }

            buffer.DrawText(x + col, y, text, run.Fg, run.Bg);
            col += text.Length;
        }
    }

    /// <summary>
    /// Renders multi-line MCI text from (x,y) downward. Lines are clipped (never wrapped) so the sysop
    /// controls layout. Returns the number of rows drawn.
    /// </summary>
    public static int RenderBlock(ScreenBuffer buffer, int x, int y, int maxWidth, int maxRows, string text, Color defaultFg, Color defaultBg)
    {
        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        int rows = Math.Min(lines.Length, maxRows);
        for (int i = 0; i < rows; i++)
        {
            RenderLine(buffer, x, y + i, maxWidth, lines[i], defaultFg, defaultBg);
        }

        return rows;
    }

    /// <summary>Returns the text with all pipe codes removed (for width math or plain rendering).</summary>
    public static string Strip(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('|'))
        {
            return text ?? string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '|' && i + 2 < text.Length && TryCode(text[i + 1], text[i + 2], out _))
            {
                i += 2;
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }

    private static bool TryCode(char a, char b, out int code)
    {
        code = 0;
        if (a is >= '0' and <= '9' && b is >= '0' and <= '9')
        {
            code = ((a - '0') * 10) + (b - '0');
            return code <= 23;
        }

        return false;
    }
}
