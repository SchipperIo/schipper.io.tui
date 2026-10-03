using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Input;
using Schipper.Io.Ansi.Screen;
using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Tests;
using Schipper.Io.Tui.Themes;
using Schipper.Io.Tui.Ui;

namespace Schipper.Io.Tui.Tests.Ui;

[TestClass]
public sealed class TerminalUiTests
{
    private static (TerminalView View, InputReader Reader) Wire(FakeTerminal terminal) =>
        (new TerminalView(terminal), new InputReader(terminal));

    private static bool BufferContains(ScreenBuffer buffer, string text)
    {
        char[] row = new char[buffer.Width];
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                row[x] = buffer[x, y].Glyph;
            }

            if (new string(row).Contains(text, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    [TestMethod]
    public async Task MenuArrowDownThenEnterSelectsSecondItem()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("\x1b[B\r"); // Down, Enter
        (TerminalView view, InputReader reader) = Wire(terminal);

        var items = new[]
        {
            new MenuItem("Alpha"),
            new MenuItem("Beta"),
            new MenuItem("Gamma"),
        };

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", items);
        Assert.AreEqual(1, chosen);
    }

    [TestMethod]
    public async Task MenuHotkeySelectsItem()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("g"); // hotkey for Gamma
        (TerminalView view, InputReader reader) = Wire(terminal);

        var items = new[]
        {
            new MenuItem("Alpha", 'a'),
            new MenuItem("Beta", 'b'),
            new MenuItem("Gamma", 'g'),
        };

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", items);
        Assert.AreEqual(2, chosen);
    }

    [TestMethod]
    public async Task MenuEscapeReturnsMinusOne()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput(new byte[] { 0x1b }); // lone Escape
        (TerminalView view, InputReader reader) = Wire(terminal);

        var items = new[] { new MenuItem("Alpha"), new MenuItem("Beta") };
        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", items);
        Assert.AreEqual(-1, chosen);
    }

    [TestMethod]
    public async Task ReadLineCollectsTypedTextUntilEnter()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("Sysop\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? line = await TerminalUi.ReadLineAsync(view, reader, Theme.Default, "Handle:");
        Assert.AreEqual("Sysop", line);
    }

    [TestMethod]
    public async Task ReadLineBackspaceEditsText()
    {
        var terminal = new FakeTerminal();
        terminal.QueueInput("abc\x7fX\r"); // a b c <bs> X Enter => "abX"
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? line = await TerminalUi.ReadLineAsync(view, reader, Theme.Default, "Text:");
        Assert.AreEqual("abX", line);
    }

    [TestMethod]
    public async Task ReadLineTrailingEscapeCancelsAndLeavesFollowingKey()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput("ab\x1b");
        terminal.QueueInput("Z\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? line = await TerminalUi.ReadLineAsync(view, reader, Theme.Default, "Name:");
        KeyEvent? next = await reader.ReadKeyAsync();

        Assert.IsNull(line);
        Assert.AreEqual('Z', next!.Value.Char);
    }

    [TestMethod]
    public async Task MenuTrailingEscapeDoesNotTakeFollowingHotkey()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput("ab\x1b");
        terminal.QueueInput("Z\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        MenuItem[] items =
        [
            new MenuItem("One"),
            new MenuItem("Zulu", 'z'),
        ];

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", items);
        Assert.AreEqual(-1, chosen);
    }

    [TestMethod]
    public async Task EmptyMenuReturnsMinusOneWithoutReadingEnter()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput("\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", []);
        Assert.AreEqual(-1, chosen);
    }

    [TestMethod]
    public async Task SeparatorOnlyMenuIgnoresEnterUntilEscape()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput("\r");
        terminal.QueueInput(new byte[] { 0x1b });
        (TerminalView view, InputReader reader) = Wire(terminal);

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", [MenuItem.Separator]);
        Assert.AreEqual(-1, chosen);
    }

    [TestMethod]
    public async Task MenuScrollsSoTheLastItemIsVisible()
    {
        FakeTerminal terminal = new(80, 10);
        terminal.QueueInput(string.Concat(Enumerable.Repeat("\x1b[B", 29)) + "\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        MenuItem[] items = new MenuItem[30];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new MenuItem($"Row{i:D2}");
        }

        int chosen = await TerminalUi.MenuAsync(view, reader, Theme.Default, "Pick", items);
        Assert.AreEqual(29, chosen);
        Assert.IsTrue(BufferContains(view.Buffer, "Row29"), "the last visible menu row should show Row29");
    }

    [TestMethod]
    public async Task MenuAnchorTopOffScreenStillShowsTheTitle()
    {
        FakeTerminal terminal = new(80, 10);
        terminal.QueueInput(new byte[] { 0x1b });
        (TerminalView view, InputReader reader) = Wire(terminal);

        int chosen = await TerminalUi.MenuAsync(
            view, reader, Theme.Default, "AnchorTitle", [new MenuItem("Alpha")], anchorTop: 100);
        Assert.AreEqual(-1, chosen);
        StringAssert.Contains(terminal.Output, "AnchorTitle");
    }

    [TestMethod]
    public async Task ReadLineCursorDoesNotEraseTheNewestVisibleCharacter()
    {
        FakeTerminal terminal = new(20, 10);
        terminal.QueueInput(new string('A', 15) + "B\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? line = await TerminalUi.ReadLineAsync(view, reader, Theme.Default, "N:", maxLength: 30);
        Assert.AreEqual(new string('A', 15) + "B", line);
        StringAssert.Contains(terminal.Writes[^1], "B");
    }

    [TestMethod]
    public async Task ReadLineOnOneByOneDoesNotThrow()
    {
        FakeTerminal terminal = new(1, 1);
        terminal.QueueInput("\r");
        (TerminalView view, InputReader reader) = Wire(terminal);

        string? line = await TerminalUi.ReadLineAsync(view, reader, Theme.Default, "N:");
        Assert.AreEqual(string.Empty, line);
    }

    [TestMethod]
    public async Task MessageOnOneByOneDoesNotThrow()
    {
        FakeTerminal terminal = new(1, 1);
        (TerminalView view, InputReader reader) = Wire(terminal);

        await TerminalUi.MessageAsync(view, reader, Theme.Default, "Saved", "The post was stored.");
    }

    [TestMethod]
    public async Task Basic16MenuUsesFallbackPaletteNotTrueColorCells()
    {
        FakeTerminal terminal = new();
        terminal.QueueInput(new byte[] { 0x1b });
        (TerminalView view, InputReader reader) = Wire(terminal);
        view.Depth = ColorDepth.Basic16;

        _ = await TerminalUi.MenuAsync(view, reader, ThemeCatalog.Amber, "Pick", [new MenuItem("Alpha")]);

        foreach (Cell cell in view.Buffer.Cells)
        {
            Assert.AreNotEqual(ColorKind.TrueColor, cell.Foreground.Kind);
            Assert.AreNotEqual(ColorKind.TrueColor, cell.Background.Kind);
        }
    }
}
