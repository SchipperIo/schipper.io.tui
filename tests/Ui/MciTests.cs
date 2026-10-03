using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;
using Schipper.Io.Tui.Ui;

namespace Schipper.Io.Tui.Tests.Ui;

[TestClass]
public sealed class MciTests
{
    private static readonly Color Fg = Color.Basic(BasicColor.White);
    private static readonly Color Bg = Color.Basic(BasicColor.Black);

    [TestMethod]
    public void StripRemovesPipeCodes()
    {
        Assert.AreEqual("Hello world", Mci.Strip("|10Hello |04world"));
        Assert.AreEqual("plain", Mci.Strip("plain"));
        Assert.AreEqual("a|b", Mci.Strip("a|b"), "a lone pipe with no 2 digits is literal");
        Assert.AreEqual("|0Ahi", Mci.Strip("|0Ahi"), "codes are decimal 00-23, so |0A is literal");
        Assert.AreEqual("100%", Mci.Strip("|10100%"), "only the first two digits after | are the code");
    }

    [TestMethod]
    public void ParseLineSplitsRunsAndMapsDosColors()
    {
        IReadOnlyList<Mci.Run> runs = Mci.ParseLine("|04red|02green", Fg, Bg);

        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual("red", runs[0].Text);
        Assert.AreEqual(Color.Basic(BasicColor.Red), runs[0].Fg);   // |04 = red (DOS order)
        Assert.AreEqual("green", runs[1].Text);
        Assert.AreEqual(Color.Basic(BasicColor.Green), runs[1].Fg); // |02 = green
    }

    [TestMethod]
    public void BackgroundCodesSetBackground()
    {
        IReadOnlyList<Mci.Run> runs = Mci.ParseLine("|17hi", Fg, Bg); // |17 = bg index 1 = blue

        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual("hi", runs[0].Text);
        Assert.AreEqual(Color.Basic(BasicColor.Blue), runs[0].Bg);
        Assert.AreEqual(Fg, runs[0].Fg, "foreground stays the default");
    }

    [TestMethod]
    public void LeadingTextKeepsDefaultColorThenSwitches()
    {
        IReadOnlyList<Mci.Run> runs = Mci.ParseLine("plain|12red", Fg, Bg);

        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual("plain", runs[0].Text);
        Assert.AreEqual(Fg, runs[0].Fg);
        Assert.AreEqual(Color.Basic(BasicColor.BrightRed), runs[1].Fg); // |12 = bright red
    }

    [TestMethod]
    public void RenderBlockReportsRowsAndClipsToMax()
    {
        var buffer = new ScreenBuffer(40, 10);
        int rows = Mci.RenderBlock(buffer, 0, 0, 40, 2, "one\ntwo\nthree", Fg, Bg);
        Assert.AreEqual(2, rows, "limited to maxRows");
    }
}
