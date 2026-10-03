# Schipper.Io.Tui

BBS-style terminal widgets on `Schipper.Io.Ansi`. The package paints a `ScreenBuffer`, diff-renders it through `TerminalView`, and runs modal menus, prompts, and a small editor.

Cells, color, input decoding, and the screen buffer stay in `Schipper.Io.Ansi`. This package does not reimplement them.

## Guides

| Guide | What it covers |
| --- | --- |
| [Getting started](getting-started.md) | A view, a theme, and a menu |
| [Screens and themes](screens-and-themes.md) | `TerminalView`, `Draw`, `Theme`, `ThemeCatalog` |
| [Widgets](widgets.md) | Menus, line input, confirm, messages, MCI, the editor |

## Namespaces

| Namespace | Types |
| --- | --- |
| `Schipper.Io.Tui.Screen` | `TerminalView`, `Draw` |
| `Schipper.Io.Tui.Themes` | `Theme`, `ThemeCatalog` |
| `Schipper.Io.Tui.Input` | `InputReader` |
| `Schipper.Io.Tui.Ui` | `TerminalUi`, `MenuItem`, `Mci`, `MultilineEditor` |

`ITerminal`, `ScreenBuffer`, `KeyEvent`, and `Color` come from `Schipper.Io.Ansi`.

## Package

```xml
<PackageReference Include="Schipper.Io.Ansi" Version="0.1.0-dev" />
<PackageReference Include="Schipper.Io.Tui" Version="0.1.0-dev" />
```

Target framework: `net10.0`. Pack Ansi into the feed you restore from before building Tui. The Tui package depends on `Schipper.Io.Ansi` `0.1.0-dev`.
