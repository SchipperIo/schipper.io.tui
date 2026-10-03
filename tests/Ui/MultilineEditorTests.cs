using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Tests;
using Schipper.Io.Tui.Themes;
using Schipper.Io.Tui.Ui;

namespace Schipper.Io.Tui.Tests.Ui;

[TestClass]
public sealed class MultilineEditorTests
{
    private const byte CtrlS = 0x13;
    private const byte CtrlX = 0x18;

    private static (TerminalView View, InputReader Reader) Wire(FakeTerminal terminal) =>
        (new TerminalView(terminal), new InputReader(terminal));

    [TestMethod]
    public async Task TypingNewlinesThenCtrlSReturnsJoinedText()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("ab\rcd");           // 'a','b', Enter, 'c','d'
        terminal.QueueInput(new[] { CtrlS });     // save & exit

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.AreEqual("ab\ncd", result);
    }

    [TestMethod]
    public async Task CtrlXCancelsAndReturnsNull()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("hello");
        terminal.QueueInput(new[] { CtrlX });

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task EscapeAlsoCancels()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("hello");
        terminal.QueueInput(new byte[] { 0x1b }); // lone Escape

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ArrowLeftInsertsMidLine()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("abc");          // cursor after 'c'
        terminal.QueueInput("\x1b[D\x1b[D");  // Left, Left -> between 'a' and 'b'
        terminal.QueueInput("Z");             // insert
        terminal.QueueInput(new[] { CtrlS });

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.AreEqual("aZbc", result);
    }

    [TestMethod]
    public async Task BackspaceJoinsLines()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("ab\r");          // "ab", new line
        terminal.QueueInput("\x7f");           // Backspace at col 0 -> merge back
        terminal.QueueInput("c");              // append to merged line
        terminal.QueueInput(new[] { CtrlS });

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.AreEqual("abc", result);
    }

    [TestMethod]
    public async Task InitialTextIsEditableAndTrailingBlankLinesTrimmed()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("!");              // append '!' at end of seeded text
        terminal.QueueInput("\r\r");           // two trailing newlines
        terminal.QueueInput(new[] { CtrlS });

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: "hi");

        Assert.AreEqual("hi!", result); // trailing blank lines dropped
    }

    [TestMethod]
    public async Task CtrlSRoundTripsAThreeHundredCharacterLine()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput([CtrlS]);
        (TerminalView view, InputReader reader) = Wire(terminal);

        string initial = new('x', 300);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial);

        Assert.IsNotNull(result);
        Assert.AreEqual(300, result.Count(c => c == 'x'));
    }

    [TestMethod]
    public async Task JoinRewrapKeepsFourHundredCharacters()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput(new string('x', 200));
        terminal.QueueInput("\r");
        terminal.QueueInput(new string('x', 200));
        terminal.QueueInput("\x1b[A"); // Up
        terminal.QueueInput("\x1b[F"); // End
        terminal.QueueInput("\x1b[3~"); // Delete (join)
        terminal.QueueInput([CtrlS]);

        (TerminalView view, InputReader reader) = Wire(terminal);
        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);

        Assert.IsNotNull(result);
        Assert.AreEqual(400, result.Count(c => c == 'x'));
    }

    [TestMethod]
    public async Task EditOnOneByOneDoesNotThrow()
    {
        FakeTerminal terminal = new(1, 1);
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: "hi");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task NarrowEditorSavesTypedText()
    {
        FakeTerminal terminal = new(10, 25);
        terminal.QueueInput("abcdefgh");
        terminal.QueueInput([CtrlS]);
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? result = await MultilineEditor.EditAsync(view, reader, Theme.Default, "Body", initial: null);
        Assert.AreEqual("abcdefgh", result);
    }
}
