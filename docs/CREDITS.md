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
