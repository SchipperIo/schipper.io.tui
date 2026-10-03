# Widgets

[Index](README.md)

`TerminalUi`, `Mci`, and `MultilineEditor` run their own paint and input loops. They call `SyncSize` and `FlushAsync` on the view you pass. They share one `InputReader`, so keystrokes stay in order across calls.

## MenuItem

```csharp
var items = new MenuItem[]
{
    new MenuItem("Enter", hotkey: 'e', tag: "enter"),
    new MenuItem("Locked", hotkey: 'l', enabled: false),
    MenuItem.Separator,
    new MenuItem("Quit", hotkey: 'q'),
};
```

| Member | Meaning |
| --- | --- |
| `Label` | The row text. An empty label with `enabled: false` is a separator |
| `Hotkey` | A character the menu activates. `'\0'` means none |
| `Tag` | An optional object your code reads after the choice |
| `Enabled` | A disabled row is drawn in `MutedForeground` and cannot be selected |
| `IsSeparator` | `Label` is empty and the item is disabled |
| `MenuItem.Separator` | A ready-made separator |

## TerminalUi

Every method takes `TerminalView`, `InputReader`, `Theme`, and an optional `BackgroundPainter`:

```csharp
TerminalUi.BackgroundPainter paint = (buffer, theme) =>
{
    Draw.FillBackground(buffer, theme.Background);
    buffer.DrawText(0, 0, "Workshop", theme.HeaderForeground, theme.HeaderBackground, CellAttributes.Bold);
};
```

When `background` is null, the widget fills the buffer with `theme.Background`. Each method starts with `theme = theme.ForDepth(view.Depth)`.

### MenuAsync

```csharp
int index = await TerminalUi.MenuAsync(
    view, reader, theme, "Workshop", items, paint, anchorTop: 4, cancellationToken);
```

Returns the selected index, or `-1` on Escape, end of stream, or an empty item list. Up and Down move among enabled rows and scroll the list when it is taller than the terminal. Enter activates the selected enabled row; Enter on an empty or all-disabled list does not throw. A hotkey activates its row immediately. `anchorTop` pins the box to that row and is clamped onto the screen. Null centers it. An all-disabled list stays until Escape.

### ReadLineAsync

```csharp
string? name = await TerminalUi.ReadLineAsync(
    view, reader, theme, prompt: "Name", mask: false, maxLength: 32, background: paint, cancellationToken);
```

Returns the text, or null on Escape or end of stream. Enter returns the current text, including empty. Backspace deletes. `mask: true` displays asterisks. Characters at `maxLength` are ignored. The default maximum is 64. When the text is longer than the field, the visible tail leaves one cell for the cursor so the newest character is not erased. A field that does not fit is skipped rather than thrown.

### MessageAsync

```csharp
await TerminalUi.MessageAsync(
    view, reader, theme, "Saved", "The post was stored.", theme.SuccessForeground, paint, cancellationToken);
```

Draws a titled box and waits for one key. `messageColor` defaults to `theme.MenuForeground`.

### ConfirmAsync

```csharp
bool yes = await TerminalUi.ConfirmAsync(view, reader, theme, "Delete this post?", paint, cancellationToken);
```

Shows Yes (`y`) and No (`n`). Returns true only when Yes was chosen. Escape and end of stream return false.

## InputReader

```csharp
var reader = new InputReader(terminal);
KeyEvent? key = await reader.ReadKeyAsync(cancellationToken);
```

Returns the next `KeyEvent`, reading the terminal as needed. Returns null at end of stream. A lone ESC on a short read is delivered as `Key.Escape`, including when earlier keys arrived in the same chunk. A full 512-byte read that ends in ESC waits for the next read so a split CSI can complete. At end of stream, a pending lone ESC is returned once as Escape, and a held CR (from a CR/LF split) is returned once as Enter. The reader copies events out of `InputParser.Feed` before the next read, so the parser's reused list cannot drop them.

Use one reader for the connection. A second reader on the same terminal consumes bytes the first reader needed.

## Mci

Pipe codes in stored text set color for the following characters. `|` plus two digits:

| Codes | Effect |
| --- | --- |
| `00`–`15` | Foreground, DOS order |
| `16`–`23` | Background, DOS order `00`–`07` |

DOS order is black, blue, green, cyan, red, magenta, yellow, white, then the bright forms of those eight. `|04` is red. `|14` is yellow. `|16` is a black background. `|23` is a white background.

A `|` that is not followed by a code in that range is drawn as itself.

```csharp
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Tui.Ui;

IReadOnlyList<Mci.Run> runs = Mci.ParseLine("|12Hello |07there", theme.Foreground, theme.Background);
int rows = Mci.RenderBlock(buffer, x: 2, y: 4, maxWidth: 70, maxRows: 10, text, theme.Foreground, theme.Background);
string plain = Mci.Strip(text);
```

`RenderLine` draws one line and clips at `maxWidth`. `RenderBlock` splits on `\n` (a `\r\n` pair is normalized first), clips each line, and does not wrap. It returns the number of rows drawn, capped by `maxRows`. `Strip` removes codes so a width check sees the visible text. `Mci.Run` is `Text`, `Fg`, and `Bg`.

## MultilineEditor

```csharp
string? body = await MultilineEditor.EditAsync(
    view, reader, theme, title: "Post", initial: draft, background: paint, cancellationToken);
```

Returns the text on Ctrl-S, with trailing blank lines removed. Returns null on Ctrl-X, Escape, or end of stream. A loaded line longer than `MaxLineLength` is wrapped into chunks; if wrapping would exceed `MaxLines`, the remainder stays on the last line. Joining two lines that would exceed `MaxLineLength` re-wraps them so the characters are kept.

| Limit | Value |
| --- | --- |
| `MaxLines` | 200 |
| `MaxLineLength` | 250 |

Arrows, Home, and End move the caret. Enter splits a line. Backspace joins with the previous line at column 0. Insert still stops at `MaxLineLength`. The editor paints its own frame and a hint bar inside the view. Tiny terminals are clamped so paint does not throw.
