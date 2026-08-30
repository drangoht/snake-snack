# Map — data, tools and documents

## §Data

| Where | What |
|---|---|
| `Assets/StreamingAssets/settings.json` | Tick rate, catch-up cap, grid dimensions, queue depth, durations of the rejection feedback, **apple seed** (`0` = a fresh one at every game; any other value = bench mode, the same apples at every game) |
| `Assets/Scripts/Rules/GameSettings.cs` | The matching schema + `Validate()`, which **never corrects silently** |
| `Assets/Scripts/Core/SettingsLoader.cs` | The reading on the engine side |

⚠ The file read at runtime is the **build's** one (`Build/Windows/SnakeSnack_Data/StreamingAssets/`),
not the one in `Assets/`: that is what makes it possible to tune the tick rate without rebuilding. A
rebuild overwrites the former with the latter.

⚠ Fields in `camelCase` in `GameSettings` — `JsonUtility` matches the JSON keys to the *fields* by their
exact name. Renaming them to PascalCase would make every value fall back to its default, with no error.

⚠ Under WebGL, `StreamingAssets` is a URL: the loader reads nothing there and returns the GDD's values.

## §Tools — `tools/`

| Tool | What it does |
|---|---|
| `build.ps1` | **Builds** (Windows/web), and `-Run` chains on to the capture. The only caller of Unity |
| `configure.ps1` | Says where Unity, Python, dotnet and butler are — and what is missing |
| `environment.ps1` | Resolves and remembers those paths (`local.settings.json`, outside git). To be dot-sourced |
| `release_itch.ps1` | Publishes a version (build → butler push → commit). `/publier-itch` skill |
| `serve_web.py` | Serves `Build/Web` **with no browser cache** — indispensable after a rebuild |
| `drive_game.py` | Launches the Windows build, injects keys, captures the window |
| `generate_snake_illustration.py` | **Produces** `Assets/Resources/Illustrations/snake-menu.png` (the menu's snake). Reads the palette from `UiPalette.cs`, copies no colour. `--preview` writes it on the game's real background |
| `generate_fonts.py` | **Produces** the two `.ttf` of `Assets/Resources/Fonts/` by instancing the upstream variable Nunito. `--check` revalidates the `cmap` without rewriting anything |

⚠ **No external tool path hard-coded anywhere**: `Unity.exe` is not in the same place from one machine
to the next. Everything goes through `environment.ps1`.

## §Docs

| Question | Document |
|---|---|
| Current phase, conventions | `CLAUDE.md` (loaded automatically) |
| *Why* the game is tuned this way | `docs/GDD.md` — to fill it: `/rediger-le-gdd` skill |
| Which pitfalls lie in wait | `docs/pitfalls/<domain>.md` (index: `docs/PITFALLS_UNITY.md`) |
| What has been tested / measured | `docs/TEST_REPORT.md` |
| What has actually shipped | `docs/DEVLOG.md` |
| Publishing | `docs/RELEASE.md` + `/publier-itch` skill |
| The store page's text | `docs/ITCH_STORE_PAGE.md` |
| The visual identity (palette, typography, contrast) | `docs/ART.md` |
| A detailed brief, the history of the visual decisions | `docs/art/` |
