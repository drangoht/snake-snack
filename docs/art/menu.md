# Brief — The main menu and its illustration

Settled 2026-08-28. What the menu must **do** is in `docs/gdd/menu.md`; this file only deals with what
is seen.

## 1. Composition

Two columns, in a 1280×720 reference frame:

- **On the left**, aligned on a single edge (x = −520 from the centre): the title `SNAKE SNACK`
  (ExtraBold 64), the tagline in secondary text (SemiBold 21), then the entries (ExtraBold 30, 62 px
  apart). The entry block stays **centred on itself** whatever their number: the web menu, deprived of
  "Quit", does not become lopsided.
- **On the right**, the snake illustration (390 px square), slightly above the centre.
- **At the foot**, the reminder of the menu keys, in secondary text.

⚠ **A single left edge, not centring.** Labels of different lengths, centred, do not start at the same
x, and a menu column then reads as a failed alignment.

## 2. The selection cursor

A **red diamond** — the game's apple, the same shape and the same colour role. A player who has not
yet started a game learns at a glance what the red shape means, and the menu does not spend one more
symbol. The selected entry goes from `SecondaryText` to `HudText` **and** grows by 7 %: the selection
therefore does not rest on colour alone (§4).

## 3. The animations

| What moves | How | Why |
|---|---|---|
| Opening | global fade 0.42 s, title and tagline rise 14 px | the screen arrives, it does not appear all at once |
| Entries | cascade, 0.07 s apart, each sliding 34 px into place | the eye is led from the top to the bottom of the list, in the order it will have to read it |
| Selection | the cursor **slides** towards the target entry (exponential smoothing) | a cursor that jumps does not say where it came from |
| Illustration | drift ±8 px over 4.2 s, tilt ±1.6° over 5.3 s | two different periods: the movement never closes on itself, it does not "loop" to the eye |
| Exit | fade 0.16 s before the game appears | a hard cut menu → game reads as a reload |

⚠ **This is not flicker.** §4 forbids "periodic looping flicker over a large area": what it targets is
a variation of **opacity**. Here the illustration's opacity does not move — it travels and tilts.
Nothing flickers, and the menu stops looking like a frozen screenshot.

⚠ **Everything is on unscaled time**: the menu does not depend on game time.

## 4. The illustration

**A snake coiled in a spiral, made of the same rounded squares as the game, its head out of the last
turn and a diamond apple placed along its line of sight.**

Produced by `tools/generate_snake_illustration.py` →
`Assets/Resources/Illustrations/snake-menu.png` (512×512, transparent background). Preview on the real
game background: `docs/check-menu-illustration.png` (`--preview`).

What holds the image together, and that a retouch must not undo:

- **The same material as the game.** The body is a sequence of spaced rounded squares, like the
  segments on screen. The illustration therefore promises nothing the game does not show.
- **The spiral tells the game loop**: the body grows, coils and ends up with no room left — that is
  the pitch of GDD §1, without a line of text.
- **The apple is along the line of sight**, not merely next to it: that is what makes a scene rather
  than two neighbouring objects.
- **No colour is invented.** The generator **reads** `UiPalette.cs`: body, head, apple, background
  (the eyes) and white (the highlight) are the roles of §1. A renamed role makes the script fail
  rather than produce a stale image.
- **The tail tapers and the last segments shade towards the head colour**: the eye finds the head
  without having to follow the body.

⚠ **The image is cropped to its content, then recentred on a square.** The menu places it in a fixed
rectangle: it is the centre of the file that lands at the centre of that rectangle. Without cropping,
tweaking a constant in the generator (one more turn, a further apple) would shift the illustration in
the menu — and the fix would be made in the wrong file.

⚠ **Import.** `Assets/Editor/ImportIllustrations.cs` forces `textureType = Sprite` on everything in
`Resources/Illustrations/`. Without it, the project being in 3D mode, the PNG is imported as a
**texture**, `Resources.Load<Sprite>` returns `null`, and the menu displays with no illustration
**without raising the slightest error** (`docs/pitfalls/assets-import.md`).

## 5. The reading panels

An 880×480 card, centred, `Playfield` background, 3 px `PlayfieldBorder` frame — the same amber as the
wall that kills, so the game's frames form a family. Pause scrim over the menu. Title ExtraBold 34
centred, body SemiBold 19 **aligned left**, reminder of the back key at the foot.

⚠ **One line of Nunito takes about 1.36 times the font size**, not 1.0. Nine lines at 19 px need
~260 px of height, not 190. The first pass sized the card by naive arithmetic: the panel's last two
lines — the ones stating what kills — were truncated. The body is now **truncated inside the card**
rather than overflowing: text that is too long shows, instead of running over the frame and looking
like a rendering defect.
