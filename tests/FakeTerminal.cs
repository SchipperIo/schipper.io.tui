using System.Text;
using Schipper.Io.Ansi;

namespace Schipper.Io.Tui.Tests;

/// <summary>An in-memory <see cref="ITerminal"/> that scripts input chunks and captures output.</summary>
internal sealed class FakeTerminal : ITerminal
{
    private readonly Queue<byte[]> _input = new();
    private readonly List<string> _writes = [];
    private byte[]? _currentChunk;
    private int _currentOffset;

    public FakeTerminal(int columns = 80, int rows = 25)
    {
        Columns = columns;
        Rows = rows;
    }

    public int Columns { get; set; }

    public int Rows { get; set; }

    public event Action? Resized;

    /// <summary>All output concatenated, decoded as UTF-8.</summary>
    public string Output => string.Concat(_writes);

    /// <summary>Each <see cref="WriteAsync"/> call captured individually, decoded as UTF-8.</summary>
    public IReadOnlyList<string> Writes => _writes;

    public void QueueInput(string ascii) => _input.Enqueue(Encoding.ASCII.GetBytes(ascii));

    public void QueueInput(byte[] bytes) => _input.Enqueue(bytes);

    public void RaiseResized() => Resized?.Invoke();

    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        // Frames are written whole (the encoder flushes per write), so per-write decode is safe.
        _writes.Add(Encoding.UTF8.GetString(bytes.Span));
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_currentChunk is null || _currentOffset >= _currentChunk.Length)
        {
            if (_input.Count == 0)
            {
                _currentChunk = null;
                _currentOffset = 0;
                return ValueTask.FromResult(0);
            }

            _currentChunk = _input.Dequeue();
            _currentOffset = 0;
        }

        int n = Math.Min(_currentChunk.Length - _currentOffset, buffer.Length);
        _currentChunk.AsSpan(_currentOffset, n).CopyTo(buffer.Span);
        _currentOffset += n;
        return ValueTask.FromResult(n);
    }
}
