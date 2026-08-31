# Map — the engine code (`Gameplay/`, `UI/`)

## §Gameplay — `Assets/Scripts/Gameplay/`

| File | Responsibility |
|---|---|
| `SnakeGame.cs` | **The only MonoBehaviour that decides**: reads the keyboard, ticks the cadence, chains the states. Carries no rule — everything is delegated to `Rules/` |
| `GameState.cs` | The five states: `Waiting`, `Running`, `Paused`, `Dead`, `Won` (full grid, §4.4) |
| `BoardView.cs` | Draws the playfield, the lines, the border, the snake (reused pool), the **apple** (a diamond, a shape distinct from the snake's squares) and the rejection chevron. `Show(bool)` switches the lot off in one go when the menu takes the screen |
| `PrimitiveShapes.cs` | The 1×1 px white square the whole rendering is made of — no imported asset |
| `PersistentBest.cs` | The best between two sessions (`PlayerPrefs`, key `snakesnack.record`). ⚠ **Best effort**: read impossible → zero, write impossible → log, never a blocking error. `Save()` is explicit, otherwise a closed tab loses the best |
| `TouchInput.cs` | Reads the fingers and converts them into the requests the keyboard makes (GDD §3, touch). Judges nothing — `Rules/Swipe` and `Rules/TouchPad` do. ⚠ Converts screen px → reference px through the camera, otherwise the swipe threshold means a different gesture on every panel |
| `TouchControlsView.cs` | Draws the cross and the pause button. ⚠ Draws only: a button drawn elsewhere than it is hit-tested looks right and answers wrong |
| `Core/TouchSimulationBootstrap.cs` | `?touch` (web) / `-touch` (Windows): the mouse reports as a finger, the only way to see the mobile port without a phone. ⚠ Answers **real** clicks, never a synthetic `PointerEvent` |

The only object placed in the scene is `Game` (see `SceneBuilder.BuildGame`); `BoardView`, `GameHud`
and `MenuScreen` are added at startup by `SnakeGame`, so that no serialised reference can get lost when
the scene is regenerated.

⚠ **Canvas sorting order** — HUD 100, menu 200, build stamp 1000. Two canvases at the same order stack
according to the hierarchy, which is not stable when the scene is regenerated from code.

## §Audio — `Assets/Scripts/Audio/`

| File | Responsibility |
|---|---|
| `Sfx.cs` | The `Sfx` enum (**declares** a sound to exist) and `SfxCatalog` (**binds** it to a file and a volume). ⚠ The single place where a sound gets a file name — a moment bound to nothing is silent, and silence raises nothing |
| `MusicPlayer.cs` | The menu loop, and only the menu. ⚠ Streamed: the clip must be asked to load (`LoadAudioData`) and **waited for** — `Play()` on an unloaded clip does nothing, silently |
| `AudioMute.cs` | The player's silence, remembered (`PlayerPrefs`). ⚠ Exists because `settings.json` is not read on WebGL: it is the ONLY way to get silence on itch |
| `SfxPlayer.cs` | Loads, plays, and **audits at startup**: missing listener, unbound sound, absent file, volume at zero — each named separately. `OutputRms()` is the only proof that a sound came out, a `PlayOneShot` log proving only an intention |
| `Assets/Resources/Audio/` | The clips, loaded BY PATH. ⚠ `Resources/`, never `Art/`. Import settings forced by `Assets/Editor/ImportAudio.cs` |

Design: `docs/gdd/audio.md` (§4.7). ⚠ The `AudioListener` is added by `SceneBuilder.BuildCamera` —
it was missing entirely until 2026-08-31. **`-audiocheck` / `?audiocheck`** plays one sound and
logs the RMS that came out: the only way to prove a sound was heard rather than merely requested.

## §UI — `Assets/Scripts/UI/`

| File | Responsibility |
|---|---|
| `GameHud.cs` | Builds and drives the texts: state, controls, **permanent score and best** (top banner, §4.5), pause and death screens with their summary, the "key ignored" line. `Show(bool)` hides the whole canvas |
| `MenuScreen.cs` | The **main menu** (GDD §4.6): title, illustration, animated entries, cursor, mouse. Decides nothing — the navigation comes from `Rules/MainMenu.cs`. Raises `Confirmed` (Play, Quit) after its fade-out |
| `InfoPanel.cs` | The "How to play" and "Credits" panels: veil, amber-framed card, fade. An ordinary class, animated by `MenuScreen` |
| `UiFactory.cs` | The common bricks: canvas (with its **explicit sorting order**), text, rectangle, veil. A single place where overflow and raycast are set |
| `UiFonts.cs` | Loads the two weights and **logs** their absence — a null font draws no pixel, with no error |
| `ClickableArea.cs` | Mouse hover and click (a transparent `Image`: the game's `Text`s are not raycast targets) |
| `Assets/Resources/Illustrations/` | The menu illustration, **produced** by `tools/generate_snake_illustration.py`, imported as a Sprite by `Assets/Editor/ImportIllustrations.cs` |
| `UiText.cs` | **Every label**, in a single place. No hard-coded text anywhere else |
| `UiPalette.cs` | The **12 colour roles** of `docs/ART.md` §1. ⚠ The only place in the repository where a colour is hard-coded |
| `Assets/Resources/Fonts/` | The two Nunito weights (SemiBold, ExtraBold) + `OFL.txt`. ⚠ **Produced** by `tools/generate_fonts.py`, loaded BY PATH (`Resources.Load`) — the HUD has no serialised reference |
| `BuildStampLabel.cs` | Version stamp, on its own canvas |

Pause and death remain a veil and two lines of text over the game screen; the **menu**, on the other
hand, is a screen in its own right, which hides the board and the HUD (`Show(false)`) rather than
laying itself over them.
