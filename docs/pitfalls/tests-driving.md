# Pitfalls — Headless tests and driving the game


See the **`/check-in-game`** skill for the full procedure. The pitfalls, in short:

- **Focus is THE blocking point**: without focus, Unity receives no key and no mouse movement — the
  test lies silently. `SetForegroundWindow` alone fails from a non-interactive shell; only a **real
  click** grants focus legitimately.
- ⚠ **Even the real click fails from a background agent session** (observed on 2026-08-27:
  `tools/drive_game.py` refused to start, "Cannot give focus"). The workaround that works, in this
  order: `SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0)`, an **ALT** press-and-release —
  which brings the process into Windows's permission window —, then `AttachThreadInput` towards the
  target window's thread before `SetForegroundWindow`. Then check `GetForegroundWindow()`, without
  which everything else lies.
  ✔ **Committed into `drive_game.give_focus` on 2026-08-27**: it now chains all three means, from the
  least insistent to the most.
- ⚠ **The "a key for nothing" priming must be a key THE GAME IGNORES.** It used to be Down then Up;
  in Snake Snack, where the game starts on the first applicable direction (GDD §4.1), it started the
  game and sent the snake south **before** the scenario. The screenshot then showed a snake somewhere
  other than where the scenario placed it — with no error, and looking like a gameplay bug.
  `drive_game.prime` primes on **Tab**, bound to nothing.
- ⚠ **`PrintWindow` truncates the capture as soon as Windows applies DPI scaling**: it renders in
  logical pixels while the game draws in physical ones. You get the top-left corner of the window,
  enlarged — which looks like a game framing problem. Capture the screen (`CopyFromScreen`) and crop
  on `GetWindowRect`, after `SetProcessDPIAware()`.
- **`keybd_event` must carry the scan code** (Unity reads raw input), and **arrow keys require
  `KEYEVENTF_EXTENDEDKEY`** — without it, their scan code is the numeric keypad's and the key is lost
  silently.
- **`SetCursorPos` puts nothing into the input stream**: use `SendInput` with
  `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE`, coordinates normalised over 0..65535.
- **An instant press only tests `wasPressedThisFrame`**: anything requiring a hold requires a real
  hold. Concluding "the arrows do not work" from an instant press is wrong.
- **The Unity splash lasts ~2 s**, and **the Windows firewall opens a modal alert on the first launch
  of each new exe path** — it steals focus and greys the window.
- **Do not hard-code the position of the elements aimed at**: a redesign moves them, and clicks land
  in the void with no error at all.
- **PlayerPrefs are persistent**: driving an option with N presses gives a result *relative* to the
  previous session.
- **Pixel-analysis thresholds**: two false conclusions in a row (a centroid contaminated by a piece of
  scenery, a count of light pixels that counted the HUD text). Frame outside the HUD, exclude every
  known element by its hue, **then look at the image**.
- **What is pure geometry cannot be proven by playing**: a throwaway file in `Assets/Editor` called
  through `-executeMethod` logs the bounds to within two pixels. That is what caught a rejection zone
  three times too wide whose formula read perfectly.


**⚠ In a browser, the WINDOW's focus is not enough: the CANVAS must have it.** Observed on 2026-08-28
while driving the web build in Chrome. `give_focus()` returns `True` — the window is indeed in the
foreground — and the keys still go to the page, not to the game: nothing moves, no error. A **real
click at the centre of the canvas** is needed on top (`_foreground_by_clicking`).
⚠ And above all **not the Tab priming** of the desktop driver: in a browser, Tab **moves focus** from
one element to the next, so the priming does the exact opposite of what is asked. The click then
serves both purposes: it grants the foreground AND keyboard focus to the canvas.

**⚠ A key scenario can "succeed" while producing a completely different state.** `--keys
"escape,up"` was meant to give a pause screen: the screenshot showed a snake in full flight. Esc only
pauses from `Running` — before the first press the game is `Waiting`, the key does nothing there, and
it is `up` that started the game. No error anywhere: the script did its job, so did the game, and the
screenshot told a different story from the test. **Write the expected state before running the
scenario, and read it back on the screenshot** — here, the banner either said "Paused" or it did not.

**⚠ The focus click of `drive_game.py` is a REAL click in the game.** As long as the game had no
clickable interface it was harmless; since the menu of 2026-08-28, it can activate whatever sits at
the centre of the window. The current menu leaves an empty space there, but any screen that put a
button in the centre would be triggered by the verification tool itself.

**⚠ The tool RESTORES the physical cursor position** after that click. If the machine's mouse rests
over a menu entry, it makes it the current selection, and the keyboard scenario that follows confirms
a different entry from the one expected. Move the cursor aside before a menu scenario:

```
py -c "import ctypes; ctypes.windll.user32.SetCursorPos(1890, 12)"
```

**⚠ "cannot write empty image" on capture means the window has disappeared**, not that the capture
failed: the game closed (or minimised) during the scenario. Read `Build/Windows/player.log` — a clean
shutdown shows there with no exception at all, which is exactly what makes it misleading.

**⚠ Between two invocations of `drive_game.py`, the game keeps running** — taking focus, priming and
exiting the script easily cost two seconds, i.e. about fifteen ticks at 8/s. A scenario written as one
command per key therefore gives the snake time to cross the grid and die: on 2026-08-28, an `escape`
meant to pause arrived on the death screen, and the screenshot showed the menu instead of the pause
screen. **Any sequence that assumes continuity fits in ONE `--keys "a,b,c"`.**

## Measuring a 100 ms animation (added 2026-08-30)

Checking a piece of juice means proving that a 90 to 220 ms envelope plays out on screen. Six
pitfalls, all met in the same session, all silent.

- ⚠ **A bounding box is fixed by its extreme points**, hence by its stray pixels: the box of the
  "snake head" hue measured **638 × 423 px for a 42 px cell**, because of a few antialiasing pixels
  from a light text at the other end of the screen. Filter on a window around the **median** of the
  matched pixels, which ignores the isolated ones.
- ⚠ **The Windows title bar (#F3F3F3) falls inside the tolerance of the HUD text (#E7EDF2).** A score
  measurement band starting too high counted 3,700 px of title bar for 800 px of score: the score's
  +39 % surface bump drowned there to under 1 %, and the measurement concluded "nothing moved" about
  a piece of feedback that worked.
- ⚠ **Pure white does not measure a flash whose opacity rises.** The offending-cell flash is only
  `FFFFFF` near its peak — and at that precise instant the end scrim (62 % black) is already laid over
  it, which brings it back to a grey. Measure **lightness** (r≈g≈b, high luminance) inside the
  playfield, never the exact colour.
- ⚠ **The box of a tilted ROUNDED square does not grow like a crisp square's**: the rounded corners
  absorb the rotation. For a side `c`, a radius `r` and an angle `θ`, predict
  `(c/2 − r)(cos θ + sin θ) + r`, not `c(cos θ + sin θ)` — otherwise you expect 47 px, you measure 44,
  and you wrongly conclude the tilt is three times too weak.
- ⚠ **Driving first then measuring misses the event.** The bite happens when the bot reaches the
  apple, that is, *during* the approach: a version that approached then started the burst ate two
  apples before the measurement began. **Measure while driving.**
- ⚠ **The sampling step bounds what can be proven.** A capture costs 50 to 85 ms: a 160 ms envelope
  may only be sampled at its start and its end, and appear absent. The score bump (160 ms) was never
  caught near its peak where the best-score one (220 ms) was caught three times — **same code, same
  method**. Prove the mechanism on the longest envelope.
- ⚠ **A bot reads the screen once per tick, no more**: reading faster than the game moves makes it
  reason about a position it has just corrected — it sends the same key five times, judges it
  ineffective, and heads back the other way.
- ⚠ **The persistent best score is borrowed, not taken**: to exercise "new best", lower
  `snakesnack.record` in `HKCU:\Software\Drangoht\Snake Snack` — then **restore it in a `finally`**.
  It is somebody's score.
- ⚠ **A Chrome tab the extension can screenshot is not a tab the game RUNS in.** Driving the web
  build through the browser extension, `document.hidden` was `true` and `requestAnimationFrame`
  fired **once per second**: Chrome freezes rAF in a backgrounded tab, so Unity WebGL advances one
  frame per second while the captures keep coming back perfectly rendered. Nothing is raised — the
  screenshots look right, the snake has simply barely moved, and a timing of 125 ms per cell means
  nothing. Worse, an `await` on rAF never resolves and the JS call **times out after 45 s**.
  **Measure `document.hidden` and the real rAF rate before trusting any timing**, and ask for the
  window to be brought to the front: the extension cannot focus it. Closing the other tabs does not
  help — what counts is the window being visible, not the tab being alone.
- ⚠ **Synthetic `KeyboardEvent`s DO get into Unity WebGL** (unlike touch events, see
  `touch-mobile.md`): a `new KeyboardEvent('keydown', {code, keyCode, bubbles})` dispatched on the
  canvas *and* on the document is read. This is the only way to drive the web build at the tick,
  because a round trip through the extension costs ~0.5 to 1 s — eight cells at 8 ticks/s. Schedule
  the whole sequence with `setTimeout` inside the page, not one call per key.
- ⚠ **Clicks injected by the extension do not actuate the on-screen touch pad**, while they do
  select a menu entry. The menu answers the *mouse*; the pad waits for a touch that the injected
  click never produces, even under `?touch`. Do not conclude the pad is broken — drive it with the
  keyboard, or with a real finger.
