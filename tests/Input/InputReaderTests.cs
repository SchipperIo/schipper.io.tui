using Schipper.Io.Ansi.Input;
using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Tests;

namespace Schipper.Io.Tui.Tests.Input;

[TestClass]
public sealed class InputReaderTests
{
    [TestMethod]
    public async Task TrailingEscapeOnShortReadIsFlushedBeforeLaterKeys()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput("ab\x1b");
        terminal.QueueInput("Z\r");
        InputReader reader = new(terminal);

        KeyEvent? a = await reader.ReadKeyAsync();
        KeyEvent? b = await reader.ReadKeyAsync();
        KeyEvent? esc = await reader.ReadKeyAsync();
        KeyEvent? z = await reader.ReadKeyAsync();

        Assert.AreEqual('a', a!.Value.Char);
        Assert.AreEqual('b', b!.Value.Char);
        Assert.AreEqual(Key.Escape, esc!.Value.Key);
        Assert.AreEqual('Z', z!.Value.Char);
    }

    [TestMethod]
    public async Task FullBufferEndingInEscapeWaitsThenDecodesUp()
    {
        FakeTerminal terminal = new();
        byte[] first = new byte[512];
        Array.Fill(first, (byte)'x');
        first[511] = 0x1b;
        terminal.QueueInput(first);
        terminal.QueueInput("[A");

        InputReader reader = new(terminal);
        for (int i = 0; i < 511; i++)
        {
            KeyEvent? ch = await reader.ReadKeyAsync();
            Assert.AreEqual('x', ch!.Value.Char);
        }

        KeyEvent? up = await reader.ReadKeyAsync();
        Assert.AreEqual(Key.Up, up!.Value.Key);
    }

    [TestMethod]
    public async Task EndOfStreamFlushesPendingLoneEscapeThenReturnsNull()
    {
        FakeTerminal terminal = new();
        byte[] first = new byte[512];
        Array.Fill(first, (byte)'x');
        first[511] = 0x1b;
        terminal.QueueInput(first);

        InputReader reader = new(terminal);
        for (int i = 0; i < 511; i++)
        {
            _ = await reader.ReadKeyAsync();
        }

        KeyEvent? esc = await reader.ReadKeyAsync();
        KeyEvent? eof = await reader.ReadKeyAsync();
        Assert.AreEqual(Key.Escape, esc!.Value.Key);
        Assert.IsNull(eof);
    }
}
