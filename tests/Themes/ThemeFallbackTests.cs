using System.Reflection;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Tui.Themes;

namespace Schipper.Io.Tui.Tests.Themes;

[TestClass]
public sealed class ThemeFallbackTests
{
    /// <summary>All public color slots on <see cref="Theme"/>, discovered so new slots are covered automatically.</summary>
    private static IEnumerable<(string Name, PropertyInfo Property)> ColorSlots() =>
        typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(Color))
            .Select(p => (p.Name, p));

    private static IEnumerable<(string Slot, Color Value)> Colors(Theme theme) =>
        ColorSlots().Select(s => (s.Name, (Color)s.Property.GetValue(theme)!));

    [TestMethod]
    public void ColorSlotDiscoveryFindsTheFullSlotSet()
    {
        // Guards the reflection-based tests below against silently testing nothing.
        Assert.IsTrue(ColorSlots().Count() >= 25, "expected at least 25 color slots on Theme");
    }

    [TestMethod]
    public void ForDepthBasic16ReturnsHandTunedAllBasicFallback()
    {
        foreach (Theme theme in ThemeCatalog.All)
        {
            Theme fallback = theme.ForDepth(ColorDepth.Basic16);
            Assert.AreNotSame(theme, fallback, $"{theme.Name} must carry a distinct 16-color fallback");
            foreach ((string slot, Color value) in Colors(fallback))
            {
                Assert.AreEqual(ColorKind.Basic, value.Kind, $"{theme.Name} fallback slot {slot} must be a basic color");
            }
        }
    }

    [TestMethod]
    public void ForDepthTrueColorReturnsTheTrueColorThemeItself()
    {
        foreach (Theme theme in ThemeCatalog.All)
        {
            Assert.AreSame(theme, theme.ForDepth(ColorDepth.TrueColor));
            foreach ((string slot, Color value) in Colors(theme))
            {
                Assert.AreEqual(ColorKind.TrueColor, value.Kind, $"{theme.Name} slot {slot} must be truecolor");
            }
        }
    }

    [TestMethod]
    public void ForDepthPalette256ReturnsTheThemeItself()
    {
        // 256-color clients get the truecolor palette downgraded algorithmically at render time.
        foreach (Theme theme in ThemeCatalog.All)
        {
            Assert.AreSame(theme, theme.ForDepth(ColorDepth.Palette256));
        }
    }

    [TestMethod]
    public void FallbackNamesMatchTheirThemes()
    {
        foreach (Theme theme in ThemeCatalog.All)
        {
            Assert.AreEqual(theme.Name, theme.Fallback16.Name);
        }
    }

    [TestMethod]
    public void CatalogNamesAndLookupAreStable()
    {
        CollectionAssert.AreEqual(new[] { "Seashell", "Amber", "Matrix" }, ThemeCatalog.Names.ToArray());
        Assert.AreSame(ThemeCatalog.Seashell, Theme.Default);
        Assert.AreSame(ThemeCatalog.Seashell, ThemeCatalog.Resolve("seashell"));
        Assert.AreSame(ThemeCatalog.Amber, ThemeCatalog.Resolve("AMBER"));
        Assert.AreSame(ThemeCatalog.Matrix, ThemeCatalog.Resolve("Matrix"));
        Assert.AreSame(Theme.Default, ThemeCatalog.Resolve(null));
        Assert.AreSame(Theme.Default, ThemeCatalog.Resolve("no-such-theme"));
    }

    [TestMethod]
    public void EveryCatalogThemeHasADistinctFallback()
    {
        Theme[] fallbacks = [.. ThemeCatalog.All.Select(t => t.Fallback16)];
        for (int i = 0; i < fallbacks.Length; i++)
        {
            for (int j = i + 1; j < fallbacks.Length; j++)
            {
                Assert.AreNotSame(fallbacks[i], fallbacks[j], "fallbacks must not be shared between themes");
                Assert.AreNotEqual(fallbacks[i].Foreground, fallbacks[j].Foreground, "fallback palettes must differ");
            }
        }
    }

    [TestMethod]
    public void NoCatalogSlotIsLeftAsTheTerminalDefaultColor()
    {
        foreach (Theme theme in ThemeCatalog.All)
        {
            foreach ((string slot, Color value) in Colors(theme))
            {
                Assert.AreNotEqual(ColorKind.Default, value.Kind, $"{theme.Name} slot {slot} is a bare default");
            }

            foreach ((string slot, Color value) in Colors(theme.Fallback16))
            {
                Assert.AreNotEqual(ColorKind.Default, value.Kind, $"{theme.Name} fallback slot {slot} is a bare default");
            }
        }
    }

    [TestMethod]
    public void FallbackDefaultsToTheThemeItself()
    {
        var plain = new Theme { Name = "Plain" };
        Assert.AreSame(plain, plain.Fallback16);
        Assert.AreSame(plain, plain.ForDepth(ColorDepth.Basic16));
        Assert.AreSame(plain, plain.ForDepth(ColorDepth.TrueColor));
    }

    [TestMethod]
    public void FallbackOfAFallbackIsItself()
    {
        foreach (Theme theme in ThemeCatalog.All)
        {
            Theme fallback = theme.Fallback16;
            Assert.AreSame(fallback, fallback.Fallback16);
            Assert.AreSame(fallback, fallback.ForDepth(ColorDepth.Basic16));
        }
    }

    [TestMethod]
    public void AmberFallbackPreservesTheOriginalBasicPalette()
    {
        Theme fallback = ThemeCatalog.Amber.Fallback16;
        Assert.AreEqual(Color.Basic(BasicColor.BrightYellow), fallback.Foreground);
        Assert.AreEqual(Color.Basic(BasicColor.Yellow), fallback.FrameForeground);
        Assert.AreEqual(Color.Basic(BasicColor.Black), fallback.MenuSelectedForeground);
        Assert.AreEqual(Color.Basic(BasicColor.Yellow), fallback.MenuSelectedBackground);
        Assert.AreEqual(Color.Basic(BasicColor.Black), fallback.Background);
    }

    [TestMethod]
    public void MatrixFallbackPreservesTheOriginalBasicPalette()
    {
        Theme fallback = ThemeCatalog.Matrix.Fallback16;
        Assert.AreEqual(Color.Basic(BasicColor.BrightGreen), fallback.Foreground);
        Assert.AreEqual(Color.Basic(BasicColor.Green), fallback.FrameForeground);
        Assert.AreEqual(Color.Basic(BasicColor.Green), fallback.MenuSelectedBackground);
    }

    [TestMethod]
    public void SeashellFallbackPreservesTheOriginalBasicPalette()
    {
        Theme fallback = ThemeCatalog.Seashell.Fallback16;
        Assert.AreEqual(Color.Basic(BasicColor.White), fallback.Foreground);
        Assert.AreEqual(Color.Basic(BasicColor.Cyan), fallback.FrameForeground);
        Assert.AreEqual(Color.Basic(BasicColor.Blue), fallback.MenuSelectedBackground);
        Assert.AreEqual(Color.Basic(BasicColor.BrightYellow), fallback.HotkeyForeground);
    }
}
