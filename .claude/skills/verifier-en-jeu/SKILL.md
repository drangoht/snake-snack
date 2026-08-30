---
name: verifier-en-jeu
description: Build Snake Snack from the command line, launch it, inject real inputs into it and capture the screen — to see that a change actually works instead of concluding that it compiles. To be invoked after any change to gameplay, UI, rendering or controls, and every time one is about to write "that should work".
---

# Verify by launching the game — Snake Snack

> **"It compiles" proves nothing about a game.** An inverted keyboard mapping, a character stuck to a
> wall, a menu that does not react, scenery rendered entirely black: none of these defects appears at
> compile time, and all of them are seen in thirty seconds on a capture of the running game.

Everything is driven **without opening the editor**.

## 1. Build

```powershell
& "tools/build.ps1"              # Windows -> Build\Windows\SnakeSnack.exe
& "tools/build.ps1" -Target web  # Web     -> Build\Web
& "tools/build.ps1" -Run         # ... then launches the game and captures the screen
```

The build enables URP, regenerates `Assets/Scenes/Game.unity` from `SceneBuilder`, compiles, and writes
its log into `Logs\build-<target>.log`.

⚠ **Do not invoke `Unity.exe` directly.** Its path is not the same from one machine to the next
(`Program Files`, or any drive through the Hub's *secondary install path*); hard-coded, it gives
"Unity.exe: The term 'Unity.exe' is not recognized as the name of a cmdlet". `build.ps1` resolves it,
remembers it, and covers the three pitfalls below.

⚠ **The build fails if the Unity editor is open** ("another Unity instance is running"): `build.ps1`
then refuses to start. **Never kill the editor** — either wait, or work on a copy of `Assets` +
`Packages` + `ProjectSettings` in the scratchpad.

⚠ In PowerShell, launching Unity through the `&` operator hands back control **immediately without
doing anything**: `Start-Process -Wait` is required.

⚠ The **first** build imports the whole project (several tens of minutes) and generates `Library/` and
`ProjectSettings/` — nothing to open in Unity Hub beforehand. The following ones are fast.

## 2. Launch, act, capture

```
py tools/drive_game.py --launch --wait 4 --capture docs/check.png
py tools/drive_game.py --keys "enter,down,down,enter" --capture docs/menu.png
py tools/drive_game.py --hold right --duration 1.2 --capture docs/movement.png
py tools/drive_game.py --close
```

The script launches the exe **windowed** (full screen makes the capture and the focus unreliable), gives
it the focus through a real click, primes with one key for nothing, then acts.

⚠ **A capture is paid for when read** (~700 tokens each, and a verification loop chains ten of them).
Two reflexes:
- **Capture a lot, open only what settles the matter.** The PNGs stay on disk: they are re-read on
  demand, they are not all scrolled through to note that the menu shows.
- The captures are reduced to 960 px wide by the script — enough to judge a position, a screen state or
  a text. `--full-resolution` only for a pixel-level detail (aliasing, fine alignment), and then only
  one.

## The eight pitfalls — each one has already produced a false conclusion

1. **The focus is THE blocking point.** Out of focus, Unity receives **no** key and no mouse movement:
   the test lies silently. `SetForegroundWindow` alone fails from a non-interactive shell — only a
   **real click** in the window gives the focus legitimately. Always check
   `GetForegroundWindow() == hwnd` before concluding anything at all.
2. **The very first key after the launch is lost.** Prime with a there-and-back.
3. **The injected keys must carry the SCAN CODE** (Unity's input system reads the raw input, not the
   virtual code), and **the arrows additionally require `KEYEVENTF_EXTENDEDKEY`**: without it, their
   scan code is that of the numeric keypad and the key vanishes with no error.
4. **`SetCursorPos` is not enough for the mouse**: it moves the cursor on screen without putting
   anything into the input stream. Use `SendInput` with `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE`,
   coordinates normalised over 0..65535, in small steps.
5. **An instant press only tests `wasPressedThisFrame`.** Anything that needs a hold (movement,
   continuous navigation) requires `--hold`. Concluding "the arrows do not work" from an instant press
   is false: it is the tool, not the game.
6. **The Unity splash lasts ~2 s**: ignore the first frames.
7. **The Windows firewall opens a modal alert at the first launch of EVERY new exe path.** It steals the
   focus and greys out the window. Close it (`Get-Process PickerHost`) then relaunch, or always rebuild
   to the same path.
8. **The settings are persistent (PlayerPrefs).** Driving an option through N presses gives a result
   *relative* to the previous session: go back to a known end, then **read the value back on screen**.

⚠ **Do not hard-code the position of the elements targeted.** An overhaul that moves a button makes the
clicks land in the void — with no error, just a capture showing something other than expected. Re-read
the position on a capture before replaying an old script.

## When the eye is not enough

- **Pixel analysis** for what is too fast or too fine ("does the ball leave the frame?").
  ⚠ Frame the swept area **outside the HUD** and exclude every known element by its hue: twice in a row,
  a threshold set too wide led to a false conclusion (a centroid contaminated by the scenery, then a
  count of light pixels that was counting the HUD's white text). **Then look at the image.**
- **A long enough capture window**: 1.5 s often falls entirely inside a pause. Sweep 20 s and more,
  analysing on the fly rather than keeping the bitmaps (90 frames ≈ 350 MB).
- **Provoke the case**: a mechanic that only triggers on demand cannot be measured at random. Compare
  three columns — before, after in passive play, after in provoked play. It is the "passive" column
  that proves nothing was broken.
- **What is pure geometry is not proven by playing**: write a **throwaway** file in `Assets/Editor`,
  call it with `-executeMethod`, log the points bracketing the bound to within two pixels — then delete
  it. That is what caught a rejection zone three times too wide, whose formula nonetheless read
  perfectly.
- **For audio, measure the output** (`AudioListener.GetOutputData` + RMS), not the calls.
- **Read the player's `-logFile`** at the end: that is where runtime exceptions come out.

## Verifying the web version

```
py tools/serve_web.py            # http://localhost:8080, with NO browser cache
```
⚠ Do not use `python -m http.server`: after a rebuild, the browser pairs the `.data` of one build with
the `.wasm` of another, and the game dies on a `RuntimeError: memory access out of bounds` three
hundred lines of offsets long, which looks nothing like a cache problem.

From Chrome (`claude-in-chrome` skill):
- **an instant press only fires `wasPressedThisFrame`** — for a hold, dispatch the event yourself (Unity
  does not filter `isTrusted`):
  ```js
  const c = document.querySelector('canvas');
  const o = {key:'a', code:'KeyA', keyCode:65, which:65, bubbles:true, cancelable:true};
  c.dispatchEvent(new KeyboardEvent('keydown', o));
  await new Promise(r => setTimeout(r, 900));
  c.dispatchEvent(new KeyboardEvent('keyup', o));
  ```
- **Desktop Chrome provides NO `Touchscreen`**: `Touchscreen.current` stays `null` and any touch code
  exits immediately, with no error. Only **`?touch`** (which enables `TouchSimulation`) makes touch
  testable — and it then responds to **real** clicks, not synthetic ones. To prove a movement button
  responds, `left_click_drag` from one point to another **of the same button**: the hold lasts long
  enough to produce a visible movement.
- **Synthetic `PointerEvent`s do not reach uGUI** (unlike `KeyboardEvent`s).
- **The itch.io iframe is cross-origin**: nothing gets in. Open the iframe's URL directly in a tab
  (`document.querySelector('iframe').src`) — there, everything becomes drivable again.

## ⚠ AZERTY keyboard

`KeyCode` (the old Input Manager) as well as `Key` (Input System) designate a **physical position on a
QWERTY keyboard**, never the printed character. `Key.W` / `Key.A` / `Key.S` / `Key.D` place the controls
under the keys marked **Z / Q / S / D** on a French keyboard — that is the intended result, not a bug.
Ban `A`, `Q`, `Z`, `W`, `M` for global shortcuts; prefer `Tab`, `R`, the digits or the arrows.
