# History of visual decisions

One entry per settled brief, in chronological order. Keep the rejected variants and their reason
rather than erasing them — the same convention as the GDD. It is only opened to reopen a decision.

<!-- One entry per settled brief, in chronological order. Keep the rejected variants and their reason
     rather than erasing them — see the GDD's convention. -->

- **2026-08-30 — The juice (§5) and the cartoon, delivered in full.** P2 and P3 of `art/juicy.md`
  (apple pop-in, best-score summary bump, head tilt on a turn) and the face of `art/cartoon.md` §3.3
  were delivered. The face was the only point left open by the cartoon brief: it feared eyes of
  "~4-5 px radius, below the readability threshold" at 44 px. Checked on a real screenshot in all four
  directions — a 4.6 px radius makes a 9 px diameter eye, clear and sharp. **Kept as-is on the
  author's decision**, without shrinking the eyes.

- **2026-08-28 — Typography, how it is obtained (§2.2). Family KEPT, sourcing changed.**
  The brief's bet is lost: Nunito **does not** have static weights on `google/fonts` (no `static/`,
  upstream in `buildStatic: false`), exactly the reason that had ruled Fredoka out — applied to its
  replacement. Two ways out were possible: change family again, or instantiate the variable file.
  **The author ruled for instantiation**: the documented pitfall targets importing a variable file
  into Unity, not an extracted instance, which is an ordinary static `.ttf`. The brief's reasoning (a
  restrained round face, neither geometric nor bubbly) had no reason to be redone over a tooling
  obstacle. Changing family would have been **ruled out** for that: it would have replayed a validated
  aesthetic choice to work around a distribution problem.
  Committed to the repository: `tools/generate_fonts.py` (versioned generator, upstream sha256 pinned,
  blocking `cmap` check), `Assets/Resources/Fonts/` (two `.ttf` + `OFL.txt`), `docs/CREDITS.md`.
  Nunito declaring **no Reserved Font Name**, the name is kept. Verified in game **and in the
  browser** (`docs/TEST_REPORT.md`, 2026-08-28).

- **2026-08-28 — Typography (§2).** Family **Nunito** (SIL OFL) adopted, two weights (SemiBold /
  ExtraBold), sizes raised by two points, floor 18 px. Variant **Fredoka** ruled out: the family only
  exists as a variable file in the `google/fonts` repository at the time this brief was written
  (`docs/pitfalls/fonts-text.md` already documents that pitfall for that exact family) — a fixed
  weight importable without risk was not guaranteed to be available. Variant **Baloo 2** ruled out:
  too round and bubbly a design, read as a game for children rather than "casual" — and its stroke,
  thinner still than Nunito's at equal weight, would have needed more compensation (weight, size) for
  a marginal gain in personality. Detail: [`art/typography.md`](typography.md).

- **2026-08-28 — Palette (§1).** Recommendation adopted: near-black cold base
  (background/playfield/grid), four warm colours each carrying a piece of gameplay information (amber
  wall, red apple, green snake, pure white pictogram). Fixes the defect observed on 2026-08-27
  (`docs/TEST_REPORT.md`): the apple/head contrast ratio goes from 1.41:1 (grey) to 3.36:1 (colour).
  Variant **"Neon on pure black"** ruled out (background `#000000`, cyan snake, magenta apple): a
  strict black crushes `GridLine` on low-end screens, the neon/sci-fi aesthetic contradicts the
  "canonical, no twist" pitch of GDD §1, and the cyan/magenta pair could have reproduced the same
  luminance-proximity defect as the original grey, with no calculation having checked it.
  Variant **"Monochrome green, retro terminal"** ruled out (every gameplay colour in a single green
  hue, CRT phosphor style): it gives up the very point of having a palette — every pair goes back to
  being distinguished by luminance alone, exactly the problem just paid for in greyscale; and the
  "hacker terminal" connotation does not match the "snack", casual framing of the title. Detail,
  numeric ratios and the point left open (apple/body under colour blindness):
  [`art/palette.md`](palette.md).

- **2026-08-28 — Band score and best score (GDD §4.5): placement set BY DEFAULT, brief still open.**
  No brief existed and the batch could not wait: score on the left of the band in main text, best
  score on the right in secondary text, summary between the title and the restart line on the end
  screen. Everything in grey (§5.6). ⚠ **This is not a settled artistic decision** — it is a developer
  choosing for lack of an art director, exactly what an empty §1 always ends up producing. To be
  picked up with the palette and the typography.
  ⚠ **Update 2026-08-28**: §1 and §2 are now settled (entries above). Wiring `GameHud.cs` onto
  `UiPalette.HudText` / `SecondaryText` remains to be done — this default placement is still not
  reviewed by an artistic decision until that wiring is.

- **2026-08-27 — Feedback for a refused input (§5).** Variant A **adopted and validated by the
  author** (pictogram anchored to the head for `ReversalRejected`/`RejectedQueueFull`, text on the
  pause screen for `RejectedGamePaused`, no feedback for `RejectedDuplicate`). Variant B (cell
  outline) ruled out for loss of directional information. Variant C (a single feedback including the
  duplicate) ruled out for the risk of noise.
