# Credits and third-party licences

Anything not produced for this project is listed here, with its licence and the attribution it
requires. ⚠ **An entry is added in the commit that introduces the item**, not at publishing time: a
forgotten licence only shows after going live.

⚠ **This file has an IN-GAME counterpart**: the main menu (GDD §4.6) carries a "Credits" entry, whose
text lives in `UiText.CreditsBody`. An attribution added here is added there too, in the same commit
— a licence text that only lives in the repository does not discharge the obligation towards a player
who will never see the repository.

## Fonts

### Nunito — SIL Open Font License 1.1

- **Authors**: Vernon Adams, Cyreal, Jacques Le Bailly.
  `Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito)`
- **Licence**: SIL Open Font License, Version 1.1. Full text committed to the repository **and
  embedded in the binary**: `Assets/Resources/Fonts/OFL.txt`.
- **Source**: `google/fonts`, `ofl/nunito/Nunito[wght].ttf` (variable file — it is the only one
  upstream publishes, it builds no static weight).
- **Modification**: static instances `wght=600` (SemiBold) and `wght=800` (ExtraBold) extracted with
  `fontTools.varLib.instancer`. The exact, reproducible procedure is `tools/generate_fonts.py` — the
  sha256 of the upstream file is pinned there.
- **Name kept**: Nunito's `OFL.txt` **declares no Reserved Font Name** (its copyright line does not
  carry the `with Reserved Font Name` suffix). Clause 5 of the SIL OFL, which would forbid a modified
  version from keeping the name, therefore does not apply.
  ⚠ **This check must be redone for any other family**: most of them declare one, and renaming then
  becomes mandatory, not optional.

## Sound effects

### Kenney — "Interface Sounds" — CC0 1.0

- **Author**: Kenney (<https://kenney.nl/assets/interface-sounds>).
- **Licence**: Creative Commons CC0 1.0 — public domain, **no attribution required**. Credited
  anyway: a pack given freely costs one line to name, and that line is what lets a reader check the
  claim.
- **Files kept**: 4 of the 100 — `drop_002` → `bite.ogg`, `click_002` → `menu-move.ogg`, `confirmation_004` →
  `menu-confirm.ogg`, `error_006` → `death.ogg`.
- **Modification**: renamed to the names of `SfxCatalog`, nothing else. ⚠ Volumes are balanced in
  the code (`SfxCatalog.Volume`), never by re-exporting a file — re-exporting loses the original,
  and the balance stops being readable in a diff.

### rubberduck — "100 CC0 SFX" — CC0 1.0

- **Author**: rubberduck (<https://opengameart.org/content/100-cc0-sfx>).
- **Licence**: Creative Commons CC0 1.0 — public domain, no attribution required.
- **Files kept**: none in the end. The pack was downloaded and measured for the bite (its two
  "plop" clips are the most literal mouth sounds of the two banks), but they came out at 300 and
  360 ms and brighter than Kenney's `drop_002`, which took the role. **Kept in the credits because
  the pack was used to choose**, and because the next pass will start from it again.

⚠ **These four clips were chosen on measurements, not by ear**: duration, attack time, spectral
centroid and spectral flatness narrowed some 200 candidates to a shortlist (the method is described
in `docs/gdd/audio.md`). Whether they are *right* is heard, not measured. **They had not been
listened to as of 2026-08-31** — whoever runs the game with sound first should judge them, and
swapping one is a matter of dropping another file under the same name.
