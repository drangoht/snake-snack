# Unity pitfalls — index

**The most valuable content in the repository.** Every entry matches a defect actually encountered,
which produced **no compile error, no exception and no warning** — only a game behaving wrongly. It
is the class of bug that takes hours to find and thirty seconds to fix.

> **This file is an index: open only the domain concerned.** It was split because it grows without
> end — reading it whole before every task cost more than the task itself. Open two or three relevant
> domains, never all fourteen.

Entries marked **[inherited]** come from earlier projects (Chimera Protocol, Smily Volley): they have
not been re-verified here, but each of them cost at least one regression.

## Where to look

| File | Open when touching… | Keywords |
|---|---|---|
| [`pitfalls/assets-import.md`](pitfalls/assets-import.md) | adding or regenerating an asset | `.meta`, GUID, `Art/` vs `Resources/`, `AssetDatabase.Refresh` |
| [`pitfalls/rendering-urp.md`](pitfalls/rendering-urp.md) | rendering, camera, light, materials | `QualitySettings`, 2D Renderer, `Light2D`, black sprite |
| [`pitfalls/fonts-text.md`](pitfalls/fonts-text.md) | font, displayed text, symbols | glyph fallback, `cmap`, arrows lost on WebGL, SIL OFL |
| [`pitfalls/inputs.md`](pitfalls/inputs.md) | controls, keyboard, gamepad | `activeInputHandler`, AZERTY / QWERTY, `InputSystemUIInputModule`, first key lost |
| [`pitfalls/interface.md`](pitfalls/interface.md) | HUD, menus, modals, navigation | canvas sorting order, focus trap, affordance |
| [`pitfalls/game-loop.md`](pitfalls/game-loop.md) | tick, speed, pause, catch-up | a cap that spreads the backlog, focus loss |
| [`pitfalls/pure-logic-tests.md`](pitfalls/pure-logic-tests.md) | `Assets/Scripts/Rules/`, `dotnet test` | a test never seen red, compilation more permissive than Unity, non-recursive csproj glob |
| [`pitfalls/build.md`](pitfalls/build.md) | `build.ps1`, versioning, build stamp | misleading return code, editor open, regenerated scene |
| [`pitfalls/build-web.md`](pitfalls/build-web.md) | WebGL target, game page | stripping, browser cache, `html5` channel, `Data/` folder |
| [`pitfalls/touch-mobile.md`](pitfalls/touch-mobile.md) | touch port, `index.html` | `maxTouchPoints`, `dvh`, `devicePixelRatio`, no `Touchscreen` on desktop |
| [`pitfalls/tests-driving.md`](pitfalls/tests-driving.md) | headless tests, `drive_game.py`, screenshots | focus, Unity splash, capture window |
| [`pitfalls/powershell.md`](pitfalls/powershell.md) | writing or changing a `.ps1` script | `$?` after a native exe, `$ErrorActionPreference`, `-DryRun` |
| [`pitfalls/audio.md`](pitfalls/audio.md) | music, sound effects, mixing | a silent lookup table, user gesture required, proving sound comes out |
| [`pitfalls/itch-publishing.md`](pitfalls/itch-publishing.md) | store page, devlog, itch.io | Redactor, Selectize, page cache, cross-origin iframe |

## Adding a pitfall

In the domain's file, **in the commit that discovered it**. Strict admission rule:

> An entry describes a defect that raises **no** error. Not a compile error, not an exception, not a
> warning — only wrong behaviour. An ordinary best practice does not belong here.

Every entry says **what happens**, **why it does not show**, and **what works**. The observed symptom
beats the abstract rule.

⚠ **A domain file that goes past ~150 lines gets split** (`build.md` → `build.md` +
`build-versioning.md`), and this table follows. Otherwise we come back to the monolith we have just
taken apart.
