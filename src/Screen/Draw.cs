using Schipper.Io.Ansi.Art;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;

namespace Schipper.Io.Tui.Screen;

/// <summary>Stateless drawing helpers for composing widgets into a <see cref="ScreenBuffer"/>.</summary>
public static class Draw
{
    private readonly record struct BoxGlyphs(char Tl, char Tr, char Bl, char Br, char H, char V);

    private static readonly BoxGlyphs Single = new(
        Glyphs.TopLeft, Glyphs.TopRight, Glyphs.BottomLeft, Glyphs.BottomRight, Glyphs.Horizontal, Glyphs.Vertical);

    private static readonly BoxGlyphs Double = new(
        Glyphs.DoubleTopLeft, Glyphs.DoubleTopRight, Glyphs.DoubleBottomLeft, Glyphs.DoubleBottomRight, Glyphs.DoubleHorizontal, Glyphs.DoubleVertical);

    /// <summary>Fills the entire buffer with spaces in the given background color.</summary>
    public static void FillBackground(ScreenBuffer buffer, Color background) =>
        buffer.Fill(new Cell(' ', Color.Default, background));

    /// <summary>Fills a rectangle with a glyph and colors.</summary>
    public static void FillRect(ScreenBuffer buffer, int x, int y, int width, int height, char glyph, Color fg, Color bg)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                buffer.Set(x + col, y + row, glyph, fg, bg);
            }
        }
    }

    /// <summary>Draws a box border. When <paramref name="doubleLine"/> is true, uses double-line glyphs.</summary>
    public static void Box(ScreenBuffer buffer, int x, int y, int width, int height, Color fg, Color bg, bool doubleLine = false)
    {
        if (width < 2 || height < 2)
        {
            return;
        }

        BoxGlyphs g = doubleLine ? Double : Single;
        int right = x + width - 1;
        int bottom = y + height - 1;

        buffer.Set(x, y, g.Tl, fg, bg);
        buffer.Set(right, y, g.Tr, fg, bg);
        buffer.Set(x, bottom, g.Bl, fg, bg);
        buffer.Set(right, bottom, g.Br, fg, bg);

        for (int col = x + 1; col < right; col++)
        {
            buffer.Set(col, y, g.H, fg, bg);
            buffer.Set(col, bottom, g.H, fg, bg);
        }

        for (int row = y + 1; row < bottom; row++)
        {
            buffer.Set(x, row, g.V, fg, bg);
            buffer.Set(right, row, g.V, fg, bg);
        }
    }

    /// <summary>Draws a box with an inset, centered title in the top border.</summary>
    public static void TitledBox(ScreenBuffer buffer, int x, int y, int width, int height, string title, Color frameFg, Color titleFg, Color bg, bool doubleLine = false)
    {
        Box(buffer, x, y, width, height, frameFg, bg, doubleLine);
        if (width < 2 || title.Length == 0)
        {
            return;
        }

        string label = $" {title} ";
        if (label.Length > width - 2)
        {
            label = label[..(width - 2)];
        }

        int tx = x + ((width - label.Length) / 2);
        buffer.DrawText(tx, y, label, titleFg, bg, CellAttributes.Bold);
    }

    /// <summary>Writes text horizontally centered on a row.</summary>
    public static void CenteredText(ScreenBuffer buffer, int y, string text, Color fg, Color bg, CellAttributes attributes = CellAttributes.None)
    {
        int x = Math.Max(0, (buffer.Width - text.Length) / 2);
        buffer.DrawText(x, y, text, fg, bg, attributes);
    }

    /// <summary>Writes text centered within a given horizontal span.</summary>
    public static void CenteredTextIn(ScreenBuffer buffer, int x, int y, int width, string text, Color fg, Color bg, CellAttributes attributes = CellAttributes.None)
    {
        if (width < 1)
        {
            return;
        }

        if (text.Length > width)
        {
            text = text[..width];
        }

        int tx = x + ((width - text.Length) / 2);
        buffer.DrawText(tx, y, text, fg, bg, attributes);
    }
}
