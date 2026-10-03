using Schipper.Io.Ansi;
using Schipper.Io.Ansi.Input;

namespace Schipper.Io.Tui.Input;

/// <summary>
/// Reads bytes from an <see cref="ITerminal"/> and decodes them into <see cref="KeyEvent"/>s,
/// buffering across reads. A lone ESC byte on a short read is treated as the Escape key, while ESC
/// that fills the read buffer is held until the rest of a sequence arrives (or the stream ends).
/// </summary>
public sealed class InputReader
{
    private readonly ITerminal _terminal;
    private readonly InputParser _parser = new();
    private readonly Queue<KeyEvent> _queue = new();
    private readonly byte[] _readBuffer = new byte[512];

    public InputReader(ITerminal terminal) => _terminal = terminal;

    /// <summary>
    /// Returns the next key, reading from the terminal as needed. Returns <c>null</c> at end of stream.
    /// </summary>
    public async ValueTask<KeyEvent?> ReadKeyAsync(CancellationToken cancellationToken = default)
    {
        while (_queue.Count == 0)
        {
            int read = await _terminal.ReadAsync(_readBuffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                // End of stream: a held CR is Enter, and a lone ESC is Escape. A longer unfinished
                // CSI is left alone so Flush cannot turn ESC[ into a spurious Escape.
                if (_parser.PendingCount == 1)
                {
                    KeyEvent? flushed = _parser.Flush();
                    if (flushed is { } ev)
                    {
                        return ev;
                    }
                }

                return null;
            }

            // InputParser.Feed returns an internal list reused across calls — valid only until the
            // next Feed. Every event is copied into the queue here, before Feed can run again.
            foreach (KeyEvent ev in _parser.Feed(_readBuffer.AsSpan(0, read)))
            {
                _queue.Enqueue(ev);
            }

            // A trailing ESC on a short read is the Escape key, even when earlier keys were decoded.
            // A full buffer may have split ESC from a following CSI, so that case waits.
            if (_parser.PendingIsLoneEscape && read < _readBuffer.Length)
            {
                KeyEvent? flushed = _parser.Flush();
                if (flushed is { } esc)
                {
                    _queue.Enqueue(esc);
                }
            }
        }

        return _queue.Dequeue();
    }
}
