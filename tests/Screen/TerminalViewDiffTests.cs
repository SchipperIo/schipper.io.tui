using Schipper.Io.Ansi.Colors;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Tests;

namespace Schipper.Io.Tui.Tests.Screen;

[TestClass]
public sealed class TerminalViewDiffTests
{
    private const string SyncBegin = "\x1b[?2026h";
    private const string SyncEnd = "\x1b[?2026l";
    private const string ClearScreen = "\x1b[2J";

    private static void AssertSyncWrapped(string frame)
    {
        StringAssert.StartsWith(frame, SyncBegin, "frames must open synchronized output");
        StringAssert.EndsWith(frame, SyncEnd, "frames must close synchronized output");
    }

    [TestMethod]
    public async Task FirstFlushIsSyncWrappedClearingFullRender()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        view.Buffer.DrawText(2, 1, "hello", Color.Rgb(200, 220, 230), Color.Rgb(8, 20, 34));

        await view.FlushAsync();

        Assert.AreEqual(1, terminal.Writes.Count);
        string frame = terminal.Writes[0];
        AssertSyncWrapped(frame);
        StringAssert.Contains(frame, ClearScreen, "first paint clears the screen");
        StringAssert.Contains(frame, "hello");
        // A full render covers every cell of the 80x25 screen.
        Assert.IsTrue(frame.Length >= 80 * 25, $"full frame was only {frame.Length} chars");
    }

    [TestMethod]
    public async Task SingleCellChangeSendsOnlyASmallDiff()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        await view.FlushAsync();
        int fullLength = terminal.Writes[0].Length;

        view.Buffer.Set(3, 2, 'Z', Color.Rgb(245, 200, 122), Color.Rgb(8, 20, 34));
        await view.FlushAsync();

        Assert.AreEqual(2, terminal.Writes.Count);
        string diff = terminal.Writes[1];
        AssertSyncWrapped(diff);
        StringAssert.Contains(diff, "Z");
        Assert.IsTrue(diff.Length < fullLength / 10, $"one-cell diff was {diff.Length} chars vs {fullLength} full frame");
    }

    [TestMethod]
    public async Task UnchangedFrameWritesNothing()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        view.Buffer.DrawText(0, 0, "static", Color.Rgb(255, 176, 0), Color.Rgb(16, 10, 4));
        await view.FlushAsync();

        await view.FlushAsync();
        await view.FlushAsync();

        Assert.AreEqual(1, terminal.Writes.Count, "identical frames must produce zero writes");
    }

    [TestMethod]
    public async Task ResizeForcesFullRepaint()
    {
        var terminal = new FakeTerminal(columns: 80, rows: 25);
        var view = new TerminalView(terminal);
        await view.FlushAsync();

        terminal.Columns = 100;
        terminal.Rows = 30;
        Assert.IsTrue(view.SyncSize());
        Assert.AreEqual(100, view.Columns);
        Assert.AreEqual(30, view.Rows);

        await view.FlushAsync();

        Assert.AreEqual(2, terminal.Writes.Count);
        string frame = terminal.Writes[1];
        AssertSyncWrapped(frame);
        Assert.IsTrue(frame.Length >= 100 * 30, "a post-resize flush must rewrite every cell of the new geometry");
    }

    [TestMethod]
    public async Task InvalidateForcesFullRepaintEvenWhenNothingChanged()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        await view.FlushAsync();

        view.Invalidate();
        await view.FlushAsync();

        Assert.AreEqual(2, terminal.Writes.Count);
        string frame = terminal.Writes[1];
        AssertSyncWrapped(frame);
        Assert.IsTrue(frame.Length >= 80 * 25, "Invalidate must rewrite every cell");
    }

    [TestMethod]
    public async Task DriftGuardForcesFullRepaintAfterOneHundredDiffFrames()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        await view.FlushAsync(); // full paint
        int fullLength = terminal.Writes[0].Length;

        // Exactly FullRepaintInterval changed frames flush as diffs...
        for (int i = 0; i < TerminalView.FullRepaintInterval; i++)
        {
            view.Buffer.Set(0, 0, (char)('A' + (i % 26)), Color.Rgb(80, 200, 110), Color.Rgb(3, 9, 5));
            await view.FlushAsync();
            Assert.IsTrue(terminal.Writes[^1].Length < fullLength / 10, $"frame {i + 1} should have been a diff");
        }

        // ...and the next flush is the drift-guard full repaint.
        view.Buffer.Set(0, 0, '!', Color.Rgb(80, 200, 110), Color.Rgb(3, 9, 5));
        await view.FlushAsync();

        string frame = terminal.Writes[^1];
        AssertSyncWrapped(frame);
        Assert.IsTrue(frame.Length >= 80 * 25, "the drift guard must rewrite every cell");
    }

    [TestMethod]
    public async Task UnchangedFramesDoNotAdvanceTheDriftGuard()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal);
        await view.FlushAsync();

        // Skipped (empty-diff) frames send nothing and must not count toward the interval.
        for (int i = 0; i < TerminalView.FullRepaintInterval * 2; i++)
        {
            await view.FlushAsync();
        }

        Assert.AreEqual(1, terminal.Writes.Count);
    }

    [TestMethod]
    public async Task Basic16DepthEmitsNoExtendedColorSequences()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal) { Depth = ColorDepth.Basic16 };
        Draw.FillBackground(view.Buffer, Color.Rgb(8, 20, 34));
        view.Buffer.DrawText(1, 1, "shallow", Color.Rgb(96, 226, 230), Color.Rgb(8, 20, 34));
        view.Buffer.Set(5, 5, '*', Color.Palette(214), Color.Palette(17));

        await view.FlushAsync();

        string frame = terminal.Output;
        StringAssert.Contains(frame, "\x1b[");
        Assert.IsFalse(frame.Contains("38;2", StringComparison.Ordinal), "Basic16 output must not carry truecolor foregrounds");
        Assert.IsFalse(frame.Contains("48;2", StringComparison.Ordinal), "Basic16 output must not carry truecolor backgrounds");
        Assert.IsFalse(frame.Contains("38;5", StringComparison.Ordinal), "Basic16 output must not carry 256-palette foregrounds");
        Assert.IsFalse(frame.Contains("48;5", StringComparison.Ordinal), "Basic16 output must not carry 256-palette backgrounds");
    }

    [TestMethod]
    public async Task TrueColorDepthEmitsRgbSequences()
    {
        var terminal = new FakeTerminal();
        var view = new TerminalView(terminal); // Depth defaults to TrueColor
        view.Buffer.DrawText(1, 1, "deep", Color.Rgb(96, 226, 230), Color.Rgb(8, 20, 34));

        await view.FlushAsync();

        StringAssert.Contains(terminal.Output, "38;2;96;226;230");
    }
}
