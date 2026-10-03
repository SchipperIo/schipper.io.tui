# Getting started

[Index](README.md)

`TerminalView` owns the buffer for an `Schipper.Io.Ansi.ITerminal`. `InputReader` turns the byte stream into `KeyEvent` values. `TerminalUi` runs a modal on top of both.

```csharp
using Schipper.Io.Ansi;
using Schipper.Io.Tui.Input;
using Schipper.Io.Tui.Screen;
using Schipper.Io.Tui.Themes;
using Schipper.Io.Tui.Ui;

ITerminal terminal = session; // your ITerminal
var view = new TerminalView(terminal);
var reader = new InputReader(terminal);
Theme theme = Theme.Default.ForDepth(view.Depth);

await view.BeginAsync(cancellationToken);

int choice = await TerminalUi.MenuAsync(
    view,
    reader,
    theme,
    title: "Workshop",
    items:
    [
        new MenuItem("Enter", 'e'),
        new MenuItem("Settings", 's'),
        MenuItem.Separator,
        new MenuItem("Quit", 'q'),
    ],
    cancellationToken: cancellationToken);

await view.EndAsync(cancellationToken);
```

`BeginAsync` switches to the alternate screen, hides the cursor, turns autowrap off, and clears. `EndAsync` restores autowrap and the cursor and leaves the alternate screen. Call them as a pair around the whole UI, including when the task faults. `try` / `finally` is the usual shape:

```csharp
await view.BeginAsync(cancellationToken);
try
{
    await RunAsync(view, reader, theme, cancellationToken);
}
finally
{
    await view.EndAsync(CancellationToken.None);
}
```

`MenuAsync` returns the chosen index, or `-1` when the user presses Escape, the terminal closes, or the item list is empty. Each modal applies `theme.ForDepth(view.Depth)` so a Basic16 session paints the catalog fallback instead of truecolor cells.

The host supplies `ITerminal`. This package does not open a console. An SSH session, a test double, or another byte pipe all work as long as `Columns`, `Rows`, `ReadAsync`, `WriteAsync`, and `Resized` match `Schipper.Io.Ansi.ITerminal`.

## A frame you paint yourself

Widgets are optional. Draw into `view.Buffer` and flush:

```csharp
using Schipper.Io.Ansi.Art;
using Schipper.Io.Ansi.Colors;
using Schipper.Io.Ansi.Screen;

view.SyncSize();
ScreenBuffer buffer = view.Buffer;
Draw.FillBackground(buffer, theme.Background);
Draw.TitledBox(buffer, 2, 1, 40, 8, "Status", theme.FrameForeground, theme.TitleForeground, theme.Background, doubleLine: true);
buffer.DrawText(4, 3, "Ready", theme.Foreground, theme.Background);
await view.FlushAsync(cancellationToken);
```

`FlushAsync` sends a full frame on the first paint and a diff after that. An empty diff writes nothing.

See [Screens and themes](screens-and-themes.md) and [Widgets](widgets.md).
