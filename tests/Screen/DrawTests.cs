using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;
using Schipper.Io.Tui.Screen;

namespace Schipper.Io.Tui.Tests.Screen;

[TestClass]
public sealed class DrawTests
{
    [TestMethod]
    public void TitledBoxWithWidthOneDoesNotThrow()
    {
        ScreenBuffer buffer = new(10, 5);
        Draw.TitledBox(buffer, 0, 0, 1, 5, "Title", Color.Default, Color.Default, Color.Default);
    }
}
