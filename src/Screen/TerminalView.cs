using System.Text;
using Schipper.Io.Ansi;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;
using AnsiCodes = Schipper.Io.Ansi.Sequences.Ansi;

namespace Schipper.Io.Tui.Screen;

/// <summary>
/// Owns a <see cref="ScreenBuffer"/> for a terminal and flushes frames to it. Tracks the terminal
/// size and resizes the buffer on resize. Widgets draw into <see cref="Buffer"/>; call
/// <see cref="FlushAsync"/> to send the frame. Rendering is diff-based: each flush sends only the
/// cells that changed since the previous frame, wrapped in synchronized-output (DEC 2026) so the
/// frame presents atomically. A full repaint (every cell rewritten) happens on the first paint,
/// after <see cref="Invalidate"/>, after a resize, and every <see cref="FullRepaintInterval"/> diff
/// frames as a drift guard — so nothing from a door's raw ANSI or a dropped byte can linger.
/// </summary>
public sealed class TerminalView
{
    /// <summary>A drift-guard full repaint is forced after this many consecutive diff frames.</summary>
    public const int FullRepaintInterval = 100;

    // The bytes Ansi.BeginSync emits, as a string so it can be Insert(0)-ed into the frame builder.
    private static readonly string SyncBegin = AnsiCodes.BeginSync(new StringBuilder()).ToString();

    private readonly ITerminal _terminal;
    private readonly StringBuilder _frame = new();
    private ScreenBuffer _previous; // last flushed frame; compared against Buffer to build diffs
    private bool _painted;          // false → next flush is a clearing RenderFull (nothing valid on screen)
    private bool _forceFull;        // Invalidate()/resize → next flush rewrites every cell
    private int _diffsSinceFull;    // consecutive diff frames sent since the last full repaint

    public TerminalView(ITerminal terminal)
    {
        _terminal = terminal;
        int cols = Math.Max(1, terminal.Columns);
        int rows = Math.Max(1, terminal.Rows);
        Buffer = new ScreenBuffer(cols, rows);
        _previous = new ScreenBuffer(cols, rows);
    }

    /// <summary>The working buffer widgets draw into.</summary>
    public ScreenBuffer Buffer { get; }

    /// <summary>
    /// The color capability of the remote terminal. Every render downgrades cell colors to this
    /// depth before emitting SGR, so truecolor themes degrade gracefully on 16/256-color clients.
    /// </summary>
    public ColorDepth Depth { get; set; } = ColorDepth.TrueColor;

    public int Columns => Buffer.Width;

    public int Rows => Buffer.Height;

    /// <summary>
    /// Enters the alternate screen, hides the cursor, disables autowrap, and clears. Autowrap
    /// (DECAWM) must be OFF for a sticky full-screen layout: with it on, writing the bottom-right
    /// cell scrolls the whole screen up a line, stranding the footer and cascading every row into
    /// garbage. The renderer positions every cell explicitly, so it never needs autowrap.
    /// </summary>
    public async ValueTask BeginAsync(CancellationToken cancellationToken = default)
    {
        await _terminal.WriteAsync("\x1b[?1049h\x1b[?25l\x1b[?7l\x1b[2J\x1b[H", cancellationToken).ConfigureAwait(false);
        _painted = false;
        _diffsSinceFull = 0;
    }

    /// <summary>Restores autowrap and the cursor, then leaves the alternate screen.</summary>
    public async ValueTask EndAsync(CancellationToken cancellationToken = default)
    {
        await _terminal.WriteAsync("\x1b[0m\x1b[?7h\x1b[?25h\x1b[?1049l", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forces the next <see cref="FlushAsync"/> to rewrite every cell instead of diffing. Call after
    /// something (an ANSI editor, a door game) wrote to the terminal outside this view, so the whole
    /// screen is repainted rather than trusted to match <see cref="Buffer"/>'s previous frame.
    /// </summary>
    public void Invalidate() => _forceFull = true;

    /// <summary>Resizes the buffer if the terminal size changed; returns true when a resize happened.</summary>
    public bool SyncSize()
    {
        int cols = Math.Max(1, _terminal.Columns);
        int rows = Math.Max(1, _terminal.Rows);
        if (cols == Buffer.Width && rows == Buffer.Height)
        {
            return false;
        }

        Buffer.Resize(cols, rows);
        _previous.Resize(cols, rows);
        _forceFull = true; // size changed → repaint every cell of the new geometry
        return true;
    }

    /// <summary>
    /// Sends the frame to the terminal. The normal path renders a diff against the previously
    /// flushed frame; when nothing changed, nothing is written at all. Full repaints (first paint
    /// clears via RenderFull; Invalidate/resize/drift-guard rewrite in place via RenderComplete)
    /// still send every cell. The frame is rendered into a reused builder first and only wrapped in
    /// synchronized output (DEC 2026) and sent when non-empty — that keeps the "nothing changed"
    /// check a simple length test with no bare sync-wrapper bytes on the wire.
    /// </summary>
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        bool full = !_painted || _forceFull || _diffsSinceFull >= FullRepaintInterval;
        if (full)
        {
            if (_painted)
            {
                ScreenRenderer.RenderComplete(Buffer, _frame, Depth);
            }
            else
            {
                ScreenRenderer.RenderFull(Buffer, _frame, Depth);
            }

            _painted = true;
            _forceFull = false;
            _diffsSinceFull = 0;
        }
        else
        {
            ScreenRenderer.RenderDiff(_previous, Buffer, _frame, Depth);
            if (_frame.Length == 0)
            {
                return; // frame identical to what is on screen → zero bytes on the wire
            }

            _diffsSinceFull++;
        }

        _frame.Insert(0, SyncBegin);
        AnsiCodes.EndSync(_frame);
        await _terminal.WriteAsync(_frame, cancellationToken).ConfigureAwait(false);

        // Remember what is now on screen. Copy (not swap): widgets may mutate Buffer incrementally,
        // so Buffer must keep holding exactly the frame that was just flushed.
        _previous.Blit(Buffer, 0, 0);
    }
}
