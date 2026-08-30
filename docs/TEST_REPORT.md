# Test report — Snake Snack

**Cumulative** file. Every session adds a section **at the top** (most recent first), dated, with the
version tested.

> **Never rewrite a past section.** If an old conclusion is refuted, add the refutation and **mark the
> old one as such**: the reasoning that led to the mistake is worth as much as the correction. This
> file is what keeps a known bug from being reported twice and a settled test from being redone.

> ⚠ The sessions before v0.3.0 quote the interface **as it then was, in French**. The strings have
> since been translated; the observations about them still hold.

## Session of 2026-08-30 (2) — v0.3.0-d8c811c+ (Windows build, touch simulation)

**Scope**: the touch port reopened the same day (GDD §3) — the on-screen pad, the swipe, the pause
button, tap to resume and tap to restart, and the labels that name a control. **Not tested**: a real
phone (none available), the web build's `?touch` switch after its fix, portrait orientation, and
multi-touch (two fingers at once).

**Method**: `tools/build.ps1`, then the exe launched with **`-touch`**, which turns the mouse into a
finger (`Core/TouchSimulationBootstrap.cs`). A throwaway script (not committed to `tools/`) clicks and
drags at coordinates given in the 1280×720 reference frame, converted through the window's **client**
origin — the window rect would put every click 38 px too high. Captures: `docs/verif-touch-pad.png`,
`verif-touch-swipe.png`, `verif-touch-pause.png`, `verif-touch-tap.png`.

**The point of method**: every control's position was computed from `Rules/TouchPad` and **written
down before the first capture**, in pixels of the frame. Measured on screen: North (1199, 576) ·
South (1199, 690) · West (1144, 632) · East (1256, 632) · pause (97, 134), against (1200, 577) ·
(1200, 691) · (1143, 634) · (1257, 634) · (98, 135) predicted. Playfield border at 1112, first key at
1118: the 3.5 px of clearance the rule promises are there.

| Feedback | Predicted | Measured | |
|---|---|---|---|
| Pad steers | South key → game starts, snake turns south | banner emptied, snake vertical, 4 cells down | ✔ |
| Swipe steers | downward swipe → same signature | banner emptied, snake vertical, 5 cells down | ✔ |
| Pause button | "PAUSED" veil + touch subtitle | veil, "Tap to resume - pause button for the menu" | ✔ |
| Tap resumes, tap restarts | fresh game, score 0, snake centred | exactly that | ✔ |
| Menu by tap | "Play" answers a tap | game opened | ✔ |
| Labels | no key named anywhere | "Tap an entry", "Swipe or use the pad" | ✔ |
| Controls off the playfield | no overlap, 3.5 px clear | measured above | ✔ |

### [BUG-004] — FOUND AND FIXED IN THE SAME SESSION: the pad drew perfectly and steered nothing
Severity: Blocking (the whole point of the port)
Context: first run of the touch build, `-touch`, on the game screen.
Observed / Expected: expected the snake to turn on a pad press; obtained a pad drawn to the pixel, and
a snake that never moved. Swipe likewise.
Reproduction: click any pad key; nothing happens, no error, no log.
Cause, found by instrumenting `Poll()`: **`TouchControl.phase` is a state, not an event.** `Began` was
reported on **six consecutive frames** for a single click, so one thumb queued six turns, and the
depth-2 queue of §4.2 spent its whole budget rejecting them as duplicates. The press is the
*transition*; detecting it needs the `touchId` too, the slots being a reused pool.
Assigned to: fixed on the spot in `Gameplay/TouchInput.cs`; the pitfall is in
`docs/pitfalls/touch-mobile.md`.

### Two failures that were MINE, not the game's
Recorded because each cost a full diagnostic round, and both will happen again:
- **The first "pad is dead" verdict was a test aiming at the middle of the cross**, which `TouchPad`
  answers `None` on purpose. The click was at frame y = 596 — the pad centre — where the South key is
  at 653. The code was right the whole time.
- **The first "swipe is dead" verdict was a drag built on `SetCursorPos`**, which moves the cursor and
  puts nothing in the input stream: simulation saw the press and no travel. `SendInput` with
  `MOUSEEVENTF_MOVE | ABSOLUTE` fixed it, and the swipe worked first try afterwards.

### [BUG-005] The `?touch` switch never fires on the web build
Severity: Minor (affects testing, not players)
Context: web build served locally, URL `?touch`.
Observed / Expected: expected the log line "Touch simulation enabled"; the browser console has no
trace of it, while the **same build on Windows prints it**. The mouse therefore stays a mouse, and the
port cannot be exercised in a browser.
Hypothesis: IL2CPP managed stripping is free to remove a class nothing references, and
`Application.absoluteURL` is not documented as populated at `BeforeSceneLoad`.
Two fixes attempted, **neither of which took**: calling the bootstrap explicitly from
`SnakeGame.Awake` (settles both hypotheses above), then reading every `Touchscreen` rather than just
`Touchscreen.current` (which was a real defect, see below, but not this one). After three rebuilds the
browser still does not steer from a mouse drag, while the **same code steers from the pad, the swipe,
the pause and the taps on Windows**.
⚠ **Left OPEN, deliberately.** It costs a ten-minute build per attempt and it blocks **testing only**:
a phone has a real touchscreen and real fingers, neither of which needs simulation. What it does cost
is the ability to exercise the port in a browser, which is where the game is actually published — so
it is worth reopening before the next touch change.
Assigned to: `developpeur`.

### The multi-device defect, found while chasing BUG-005 — and it WOULD have hit players
`Poll()` read `Touchscreen.current`. A machine can carry several touchscreens: this one has a device
that never receives anything, alongside whatever else appears. Reading only `current` polls the idle
one, and the game answers nothing — pad drawn, labels in touch mode, no error. Fixed by walking every
device and preferring the one actually carrying a touch. ⚠ The first attempt at that fix used
`Touchscreen.all`, which is the **inherited `InputDevice.all`** — every device, keyboard included:
`Available` would then have reported a touchscreen on any machine at all. Caught by the compiler only
because of an unrelated type error on the same line.

### Feel
**The pad is where a right thumb already is**, and it costs the playfield nothing: it sits in the
178 px of margin that the grid's rounding leaves empty, so no cell had to shrink for it. The keys are
deliberately quiet (55 % opacity at rest): they are a tool, not a character, and the eye stays on the
snake.

**What I have no proof of, and it is the important one: none of this has been touched by a finger.**
Every observation above comes from a mouse pretending to be one. A real thumb is wider than a cursor,
lands with travel, and arrives with the whole hand covering part of the screen — three things this
method cannot reproduce. The 54 px key is ~9 mm on a phone in landscape, above the usual floor, but
that is a calculation, not a measurement. **First test to run on a real device**, before the Mobile
friendly box is ticked.

## Session of 2026-08-30 — v0.2.0-3b2c0cc+ (Windows build)

**Scope**: the three P2/P3 juice feedbacks shipped that day (apple pop-in §7, summary bump §8, head
lean in a turn §9) **and the verification debt of P1** — gulp, tail pop, flash of the offending cell,
hitstop, micro-zoom, never seen on screen since they shipped in 0.2.0. **Not tested**: the web build
(none of these animations was replayed there), the pop of the new tail segment (see below), the win.

**Method**: two throwaway scripts (not committed to `tools/`) that import `drive_game`, take their own
captures in bursts (those of `drive_game` give the focus back and sleep 0.2 s: too slow to follow a
150 ms envelope) and analyse each image in numpy — area and bounding box per `UiPalette` hue. The tick
rate is slowed to 1.5–3 ticks/s **in the build's copy of `settings.json` only**, then restored. For §8,
the persistent best was lowered to 0 in the registry and **set back to 24 in a `finally`**. The second
script carries a **bot** that reads the position of the head and of the apple on screen and goes for
it: eating and dying at a precise instant is out of reach of a blind key script.

**The point of method**: every expected value was written **before** the measurement, in pixels.

| Feedback | Predicted | Measured | |
|---|---|---|---|
| §7 apple pop | 0 → peak at ×1.08 → rest, in 150 ms | 0 → 30×30 → 28×28 px in ~130 ms | ✔ |
| §9 lean, amplitude | box 42 → **44.4** px at 8° (*rounded* square) | 42 → 44 px | ✔ |
| §9 lean, duration | nothing readable left after ~45 % of the tick | a 135 ms bump on a 333 ms tick | ✔ |
| §9 lean, direction | left turn: top edge shifted right | +3 to +7.5 px on the left, −3.5 to −9 px on the right | ✔ |
| §5 gulp | box 42×42 → ~37×48, **constant area** | 38×46 px, area 1,616 → 1,632 px | ✔ |
| §6 flash of the offending cell | one cell lights up, ~1,760 px | 1,763 px on the image of the contact | ✔ |
| §6 hitstop | the flash precedes the end screen by at least one frame | flash at t, summary still empty; veil 68 ms later | ✔ |
| §6 micro-zoom | playfield border +1.7 % then exact return | 930 → **946** px (+1.72 %), back to 930 | ✔ |
| §8 best bump (banner) | area ×1.69 at the peak, 220 ms | 563 → 686 px (×1.22 at the sample) | ✔ |
| §8 summary bump | the same bump replayed once on opening | 1,182 → 1,570 → 1,182 px, three times running | ✔ |
| §5 score bump | area ×1.39 at the peak, 160 ms | never caught near the peak | ⚠ |
| §5 tail segment pop | +1,645 px of body spread over 140 ms | rise spread over ~1 s: inconclusive measurement | ⚠ |

**The two reservations, undressed**:
- **The score bump** is not proven *on screen*. A capture costs 50 to 85 ms; a 160 ms envelope may only
  be sampled at its start and at its end. What is proven is the mechanism: the best bump and the
  summary bump go through **the same `GameHud.ApplyBump` method**, with a larger amplitude and a longer
  duration, and both were seen.
- **The pop of the new tail segment** could not be isolated: the body's pixel count also varies with
  the glide, and its rise by one segment spreads over far more than the envelope's 140 ms. Neither
  disproved nor confirmed — to be taken up again with a measurement that follows the tail cell alone,
  or by eye on a slowed-down run.

**No bug found.** Two *measurement* pitfalls did, on the other hand, produce two false conclusions
before being corrected (a bounding box stretched to 638 px by stray pixels, a score bump drowned in the
Windows window's title bar): filed in `docs/pitfalls/tests-driving.md`.

## Session of 2026-08-28 (2) — v1.0-a735c8d+ (Windows AND web builds)

**Scope**: the typography of `ART.md` §2 actually wired (family, weights, sizes), and the fix for
BUG-002. **First session to verify in a browser**, which the previous one had not done. **Not tested**:
the death screen and the win, the itch page, touch, a narrow browser window (the sizes of §2.3 are set
for 1280×720 and *shrink* below that — never measured anywhere but full frame).

**Method**: `tools/generate_fonts.py`, then `tools/build.ps1` and `tools/build.ps1 -Target web`.
Windows: `tools/drive_game.py --keys "up,escape,down"`. Web: `tools/serve_web.py --port 8099` and a
throwaway script that launches Chrome on the URL, **clicks inside the canvas**, injects the same keys
and captures the window. Captures: `docs/verif-police-attente.png`, `docs/verif-police-pause.png`,
`docs/verif-web-accents.png`.

**The point of method that matters**: the expected value was written before the capture, in rows of
pixels. The 720 px canvas maps onto client rows 38..757; the controls reminder box, 24 px high and
anchored 14 px from the bottom, therefore has its bottom at row **755** — prediction: "no text pixel
below 755, the descender of the `g` reaching down to ~754". Measured: body of the text down to 749,
descenders on 750-753, **nothing at 754**. Before the fix, the text stopped dead at 756, cut off by the
edge.

### What works
- **Nunito is on screen**, in both its weights: the banner (`Score` / `Record` / state) and the titles
  are visibly bolder than the controls reminder and the subtitles. The hierarchy reads **without
  reading the words**, which is the whole point of §2.2.
- **BUG-002 fixed**: the descenders of `g`, `p`, `q` are whole, with the measurement above to back it.
- **The accents survive WebGL.** « Touche ignorée » shows with its `é` **in Chrome**, on the web build
  served locally. That is the only check that counts for this pitfall: on the desktop, a missing glyph
  is masked by the fallback onto system fonts, silently.
- The `cmap` of the **instanced** file (not of the upstream one) carries the **125 required
  characters** — ASCII 32-126 plus the 30 French accents — over 938 glyphs. The generator refuses to
  write if one is missing.
- Clean Unity import: `TrueTypeFontImporter`, `includeFontData: 1`, 129.6 KB embedded per weight.
  **0 warnings, 0 errors** in compilation on both builds. `dotnet test`: 157.
- The whole scenario passes **identically** in the browser: start on the first direction, pause,
  rejection line. No desktop / web divergence observed.

### [BUG-001] — RESOLVED (opened in the previous session)
Resolution chosen by the author: **instance** Nunito rather than change family.
`tools/generate_fonts.py` freezes `wght=600` and `wght=800` with `fontTools.varLib.instancer` and
writes `Assets/Resources/Fonts/`. The documented pitfall targets importing a *variable* file into
Unity; an extracted instance is an ordinary static `.ttf`. Licence checked before naming the files:
Nunito declares **no Reserved Font Name**, so the name is legally kept (`docs/CREDITS.md`). ⚠ The
finding of the previous session **remains true and must not be erased**: `google/fonts` publishes no
static weight of Nunito, and upstream does not build them.

### [BUG-002] — RESOLVED (opened in the previous session)
The anchor of the controls reminder raised from 10 to 14 px: the 24 px box fits whole, with 2 px to
spare. ⚠ **This fix does not close the underlying arbitration**: there is still **no margin below the
playfield**, the line still reads over the last row of cells and against the amber border.
`docs/gdd/grid.md` stays open with `game-designer` — neither the grid nor the layout of §3 was touched.

### [BUG-003] The build stamp is at 14 px, below the 18 px floor
Severity: Cosmetic
Context: `SceneBuilder.cs`, version stamp at the bottom right, present on every screen.
Observed / Expected: it is built at 14 px on the built-in font `LegacyRuntime.ttf`, whereas `ART.md`
§2.5 forbids "any text below 18 px at the reference resolution" and all the rest of the HUD has moved
to Nunito.
Hypothesis: the stamp was written before the brief, and it is deliberately discreet — raising it to
18 px and moving it to Nunito would make it more conspicuous than one may want.
Assigned to: `directeur-artistique` (either the stamp falls outside the scope of §2 and the brief must
say so, or it complies). **Not modified**: this is an arbitration about discretion, not a technical
fix.

### Feel
**Nunito delivers what the brief asked of it.** At 18 px the controls reminder stays comfortable where
the built-in font was turning dry; at 56 px, "PAUSE" in ExtraBold is distinctly more present than
before, without becoming childish. The "round but sober" bet is won on a desktop screen.

**What I have no proof of**: the rendering in the reduced window of an itch page. Every measurement of
this session is taken full frame at 1280×720, and §2.3 says explicitly that these sizes *shrink* below
that. The 18 px floor was set for exactly this, but it has never been looked at at the scale where it
counts. That is the first test to run after 0.1.0 goes online.

**The rejection chevron** remains what the previous session said of it: a white blob of 12 × 24 px,
readable as "something was refused", not as "*that* direction". Deferred past 0.1.0 by the author's
decision — no work started on it.

## Session of 2026-08-28 — v1.0-b5fa662+ (Windows build)

**Scope**: the palette of `ART.md` §1 wired into `UiPalette.cs`, and the text sizes of §2.3 wired into
`GameHud`. Two documentation claims to confirm on a real build: the rejection pictogram laid over the
snake's body (`art/rejection-feedback.md` §5.6) and the apple made readable by red (session of
2026-08-27 below). **Not tested**: the font weights (Nunito blocked, see below), the web build and the
itch page, the pause screen, the death screen, the win.

**Method**: `tools/build.ps1`, then `tools/drive_game.py --launch --wait 6 --keys left
--full-resolution`. Captures: `docs/verif-palette-attente.png` (waiting screen) and
`docs/verif-refus-chevron.png` (U-turn refused). The pictogram lives only **250 ms**: impossible to
catch with the tool, which waits 0.25 s after a key then captures. `rejectionDisplaySeconds` and
`rejectionExtensionCapSeconds` were therefore raised to **6 s** in
`Build/Windows/SnakeSnack_Data/StreamingAssets/settings.json` (the build's file, not the one in
`Assets/`), and the defaults put back afterwards. The pictogram is then captured **on its opacity
plateau**, at 1.0 — so at its most readable, which is the right frame for judging a contrast, not a
fleetingness.

**The point of method that matters**: the expected colours and the chevron's position were **written
before the first capture** (0.75 cell west of the head's centre, hence on the first body segment). The
measured chevron is centred at x = 617.5 px for a head centre at 650 px and a 45 px cell:
650 − 0.75 × 44 = 617. So what is observed is not "a pictogram appeared somewhere".

### What works
- **The 12 roles are on screen, and the brief's ratios are found again by measurement** (WCAG computed
  on the capture's pixels, not on the hex codes): apple/head **3.35** (brief 3.36) · head/body **3.17**
  (3.15) · border/playfield **7.94** (8.06) · hudText/background **16.40** (16.40) ·
  secondaryText/background **6.13** (6.13) · pictogram/body **3.81** (3.72) · pictogram/playfield
  **17.82** (17.80). No deviation beyond rounding.
- **§5.6 is closed: pure white is enough.** The chevron does land on the green body, in full `#FFFFFF`
  (154 pure white pixels measured, no attenuation), and it is seen immediately. The "dark outline"
  fallback is not necessary and was not applied.
- **The apple is no longer confused with the head**: from 1.41 : 1 in grey (previous session) to
  **3.35 : 1**. It is the only red object on screen; the eye goes to it without searching.
- A U-turn typed before the start **does not start the game** (GDD §4.1): the banner still shows
  « Une direction pour commencer » and the snake has not moved a pixel between the two captures.
- The roles laid on a uGUI `Image`/`Text` and on the camera background come out **pixel exact**; those
  laid on a `SpriteRenderer` come out 1 to 2 units darker on R and G (reported in `art/palette.md` §1.4
  — under 1 %, of no consequence).
- Build with no error and no new warning; `dotnet test`: **157 green**.

### [BUG-001] Nunito exists only as a variable file: the typography cannot be wired
Severity: Major (blocks half of `ART.md` §2)
Context: font import, before any download.
Reproduction: `GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` — no `static/`
folder, only `Nunito[wght].ttf` and `Nunito-Italic[wght].ttf`. And `ofl/nunito/upstream_info.md` notes
`buildStatic: false` in the upstream `config.yaml`: the statics are not late in being published, they
are **never built**.
Observed / Expected: expected `static/Nunito-SemiBold.ttf` and `static/Nunito-ExtraBold.ttf`; got two
variable files. That is the very condition on which brief §2.2 made Nunito conditional, and exactly
what had ruled Fredoka out.
Hypothesis: none — upstream declares it.
Consequence: the HUD stays on `LegacyRuntime.ttf`. **Nothing was improvised** (neither an instance of
the variable, nor a weight subset). The **sizes** of §2.3 are wired, the **weights** are not. The
`cmap` table was not probed: no file was fetched.
Assigned to: `directeur-artistique` (choose a family whose `static/` is listed, or explicitly settle
the use of a static instance extracted from the variable).

### [BUG-002] The descenders of the controls reminder are cut by the bottom edge
Severity: Cosmetic
Context: the game screen, permanently. Already described in `docs/gdd/grid.md` (missing bottom margin)
— this session **measures** it instead of observing it.
Reproduction: launch the game, look at "diriger", "Echap", "pause" at the bottom of the screen.
Observed / Expected: the text box is anchored 10 px from the bottom and is 24 px high, so its bottom
falls **2 px below the screen edge**; the descenders of `g`, `p`, `q` are cut off, and the line
overlaps the playfield's amber border, which occupies the very last row of pixels.
Hypothesis: it is not the text size that cuts — the cut existed at 15 px and exists at 18 px since
`ART.md` §2.3 — it is the anchor, and the bottom margin the layout did not reserve.
Assigned to: `game-designer` (the arbitration in `docs/gdd/grid.md` is still open: bottom banner,
reminder in a side margin, or acceptance by raising the text).

### Feel
**The apple.** Its size (0.72 cell) is **no longer** a problem now that the colour carries: the red
diamond is the only warm object on a cold background, it is found without sweeping the grid. What was
in the way was the value neighbouring the head, not the dimensions. I do not recommend enlarging it:
making it bigger would bring it closer to a full cell and weaken the silhouette difference, which is
precisely what saves it under colour blindness.

**The rejection pictogram.** The colour is settled, the **shape** is not. At half a cell
(`Board.MaximumPictogramSize`), the barred chevron occupies 12 × 24 px on screen: it reads as a **white
blob** appearing on the snake, not as a barred chevron. The signal "something was refused" gets
through, then, but not the "it is *that* direction". Over 250 ms of real life, I doubt a player ever
makes out the drawing. This is a question of shape and scale, not of palette: I have not touched it.

**Colour blindness (§1.5 of `art/palette.md`).** Deuteranope simulation (Viénot 1999) applied to the
capture: apple, snake body and amber border all turn to nearby olives — hue no longer separates
anything. What remains is the shape (an isolated diamond against a chain of squares against a
continuous line) and the head's lightness, and they are enough to play. A matrix on a capture is not a
player concerned: the point stays open.

## Session of 2026-08-27 — v1.0-80a7645+ (Windows build)

**Scope**: the apple (GDD §4.4) — appearance before the first press, seeded draw, growth on the bite,
replacement within the same tick. **Not tested in game**: the win (full grid, unreachable by hand —
covered by `dotnet test` alone), death by biting oneself, the web build. The score and the best (§4.5)
do not exist yet.

**Method**: `tools/build.ps1`, then `tools/drive_game.py --launch --hold right`. Seed **543** and tick
rate **1 tick/s** set in `Build/Windows/SnakeSnack_Data/StreamingAssets/settings.json` (the build's
file, not the one in `Assets/`) — the defaults were put back afterwards.

**The point of method that matters**: the first two apples of seed 543 were **computed outside the
game**, by a reimplementation of SplitMix64 and of the enumeration walk, *before* launching the game:
`(13, 7)` then `(18, 10)`. The capture matches exactly. So what is observed is not "an apple appeared
somewhere", but the whole chain — generator, walk order, draw on the tick's final state.

### What works
- The apple is **laid down before the first press**, snake still motionless (§4.4). Seen on screen.
- It is told apart from the snake **by shape**: a diamond against squares, and smaller than the cell.
- Eating lengthens the snake **on the bite**: 3 segments before, 4 after, head on the right cell.
- The next apple appears **within the same tick**, on the predicted cell. Not one frame without an
  apple.
- The banner goes back from « Une direction pour commencer » to empty when the game starts.

### Feel
The diamond is readable but **small** (0.72 cell) and of a grey close to the head's. Nothing in the way
at 21 × 15 on a desktop screen; to be revisited when the palette of `docs/ART.md` §1 exists, and to be
looked at again on the itch page, where the image is smaller.

<!-- Template for a session, to be copied at the TOP of the file:

## Session of YYYY-MM-DD — v<version>-<sha>

**Scope**: what was tested, and what was not.
**Method**: commands used (tools/drive_game.py …), options, seed.

### What works
- …

### [BUG-XXX] Short title
Severity: Blocking / Major / Minor / Cosmetic
Context: (screen, version, options)
Reproduction: (precise steps, seed if applicable)
Observed / Expected:
Hypothesis: (probable cause if obvious)
Assigned to: developpeur | game-designer

### Feel
What the measurement cannot say. It is the only source on this point, and it has already been right
against the bench.

-->
