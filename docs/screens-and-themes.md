# Screens and themes

[Index](README.md)

## TerminalView

```csharp
var view = new TerminalView(terminal);
```

The constructor sizes `Buffer` from `terminal.Columns` and `terminal.Rows`, with a minimum of 1 by 1.

| Member | Role |
| --- | --- |
| `Buffer` | The `ScreenBuffer` widgets paint |
| `Columns`, `Rows` | The buffer size |
| `Depth` | `ColorDepth` passed to the renderer. Default `TrueColor` |
| `BeginAsync` | Alternate screen, hidden cursor, autowrap off, clear |
| `EndAsync` | Reset SGR, autowrap on, cursor shown, leave the alternate screen |
| `SyncSize` | Resizes the buffer when the terminal size changed. Returns true when it did |
| `Invalidate` | The next flush rewrites every cell |
| `FlushAsync` | Sends the frame |
| `FullRepaintInterval` | `100`. A full rewrite is forced after this many consecutive diffs |

`FlushAsync` chooses the render:

| Condition | Render |
| --- | --- |
| Nothing has been painted yet | `ScreenRenderer.RenderFull` (clears, then paints) |
| `Invalidate`, a resize, or 100 diffs since the last full paint | `ScreenRenderer.RenderComplete` (rewrites every cell, no erase) |
| Otherwise | `ScreenRenderer.RenderDiff` |

The frame is wrapped in synchronized output (DEC 2026) when it is non-empty. A frame with no changes is not written, and no empty sync wrapper is sent.

`SyncSize` copies `terminal.Columns` and `terminal.Rows` onto the buffer. Call it at the start of a frame, and from the `Resized` handler if you paint outside `TerminalUi`. The modal methods call it themselves.

`Invalidate` after something else wrote to the terminal (raw ANSI, a door, a child process). The next flush then repaints from `Buffer` instead of trusting the previous diff.

Autowrap is off for the session. The renderer positions every cell. With autowrap on, writing the bottom-right cell scrolls the screen.

`Depth` is how colors are emitted. Set it from what the remote reported:

```csharp
view.Depth = ColorDepth.Palette256;
Theme theme = ThemeCatalog.Seashell.ForDepth(view.Depth);
```

## Draw

`Schipper.Io.Tui.Screen.Draw` paints into a `ScreenBuffer`. Coordinates are zero-based cells.

| Method | Effect |
| --- | --- |
| `FillBackground(buffer, background)` | Fills every cell with a space and that background |
| `FillRect(buffer, x, y, width, height, glyph, fg, bg)` | Fills a rectangle |
| `Box(...)` | A single-line frame. `doubleLine: true` uses the double-line glyphs |
| `TitledBox(...)` | A box with the title centered in the top edge, in bold |
| `CenteredText(buffer, y, text, fg, bg)` | Centers `text` on row `y` |
| `CenteredTextIn(buffer, x, y, width, text, fg, bg)` | Centers `text` inside a horizontal span. Clips to `width` |

`Box` returns without drawing when `width` or `height` is below 2. `TitledBox` also returns when `width` is below 2 so a short title slice cannot run. Glyphs come from `Schipper.Io.Ansi.Art.Glyphs`.

## Theme

`Theme` is a set of named color slots. Build one with an object initializer, or take one from `ThemeCatalog`.

| Slot | Used for |
| --- | --- |
| `Name` | Catalog key |
| `Background`, `Foreground` | The page |
| `FrameForeground`, `FrameBackground`, `TitleForeground` | Boxes and titles |
| `MenuForeground`, `MenuBackground`, `MenuSelectedForeground`, `MenuSelectedBackground`, `HotkeyForeground` | Menus |
| `MutedForeground` | Disabled items |
| `HeaderForeground`, `HeaderBackground`, `HeaderAccent` | Headers |
| `FooterForeground`, `FooterBackground`, `FooterKeyForeground`, `FooterSeparator` | Footers |
| `StatusForeground`, `StatusBackground` | Status lines |
| `AccentForeground`, `ErrorForeground`, `SuccessForeground` | Emphasis |
| `InputForeground`, `InputBackground` | Text fields |

`Theme.Default` is `ThemeCatalog.Seashell`.

`Fallback16` is the hand-tuned 16-color variant. It defaults to the theme itself, so a theme written entirely with `Color.Basic` needs no separate fallback. `ForDepth(ColorDepth.Basic16)` returns `Fallback16`. Any other depth returns the theme unchanged. The renderer downgrades truecolor to 256 colors when `TerminalView.Depth` is `Palette256`.

## ThemeCatalog

| Member | Value |
| --- | --- |
| `Seashell` | Truecolor navy, teal, and sand. The default |
| `Amber` | Amber phosphor |
| `Matrix` | Green phosphor |
| `All` | Those three, Seashell first |
| `Names` | The `Name` of each theme |
| `Resolve(name)` | Case-insensitive lookup. Blank or unknown returns `Theme.Default` |

```csharp
Theme theme = ThemeCatalog.Resolve(user.ColorScheme);
theme = theme.ForDepth(view.Depth);
```

Each catalog theme sets `Fallback16` to the basic-color version of the same palette, so a 16-color client sees that hand-tuned scheme. `MenuAsync`, `ReadLineAsync`, `MessageAsync`, `ConfirmAsync`, and `MultilineEditor.EditAsync` call `theme.ForDepth(view.Depth)` themselves at the start of the modal.
