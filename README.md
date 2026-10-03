# Schipper.Io.Tui

A TUI widget framework for BBS-style terminal screens, built on `Schipper.Io.Ansi`. It diff-renders a
`ScreenBuffer`, wraps frames in synchronized output, and falls back cleanly when a client is stuck
at 16 colors.

- **`TerminalView`** owns the buffer. `FlushAsync` emits a full frame on the first paint and a diff
  after that. `Invalidate` forces the next frame to be full. `SyncSize` follows the terminal's
  reported size.
- **`Theme` / `ThemeCatalog`** name every color slot (frame, menu, header, footer, input, status).
  `Theme.Default` is the truecolor Seashell theme. `Amber` and `Matrix` ship beside it.
  `ForDepth` selects a hand-tuned `Fallback16` for basic-16 clients; 256-color clients keep the
  truecolor palette and let the renderer downgrade it.
- **`TerminalUi`** runs modal flows on a view: `MenuAsync`, `ReadLineAsync`, `MessageAsync`, and
  `ConfirmAsync`. Menus honor hotkeys, separators, and disabled items.
- **`InputReader`** turns the terminal byte stream into `KeyEvent`s.
- **`Draw`** paints fills, boxes, titled frames, and centered text.
- **`Mci`** renders and strips MCI color codes (`RenderLine`, `RenderBlock`, `Strip`).
- **`MultilineEditor.EditAsync`** edits a block of text in place.
- **`MenuItem`** is one menu row, or `MenuItem.Separator`.

## Documentation

Usage of the view, themes, and widgets is in [docs/](docs/README.md).

The only dependency is `Schipper.Io.Ansi` 0.1.0-dev, restored from the shared feed at `../nuget.cache`.

## Build, test, package

Builds run in the latest .NET 10 SDK container, `mcr.microsoft.com/dotnet/sdk:10.0`. Docker is required. `build.ps1` and `build.sh` both run `container.sh` inside that image. The source is copied into `/tmp` inside the container, so `bin/` and `obj/` stay off the host. A packed package is written to `./dist` and copied to the shared local feed at `../nuget.cache`.

`nuget.config` restores `Schipper.*` from that feed and every other package from nuget.org. `global.json` requests SDK 10.0.100 and rolls forward to the latest .NET 10 SDK in the image.

No flags builds Release. Flags combine. The runtime identifier defaults to `linux-x64` (`RID=win-x64 ./build.sh` or `./build.ps1 -Rid win-x64`).

```bash
./build.sh           # restore + build
./build.sh -t        # unit tests
./build.sh -i        # integration tests, if any
./build.sh -p        # pack into ./dist and ../nuget.cache
./build.sh -r        # run, when the project is an executable
./build.sh -q        # unit tests under dotnet-trace -> ./dist/trace
./build.sh -o        # also write build/test logs to ./dist/raw
./build.sh -t -p     # flags combine
```

```powershell
./build.ps1
./build.ps1 -t -p
```

Pack `Schipper.Io.Ansi` into `../nuget.cache` before building this project. `TerminalUi`, `Draw`, and the themes are a library, so `-r` does nothing here.

## Continuous integration

`.github/workflows/build.yml` runs on Ubuntu for every push and pull request. It checks out `SchipperIo/schipper.io.ansi`, packs that package into the runner's local feed, then runs `./build.sh -t -p`. The packed `Schipper.Io.Tui` package is uploaded as the `nuget` workflow artifact.

## License

Licensed under the MIT License. See [LICENSE](LICENSE) for details.
