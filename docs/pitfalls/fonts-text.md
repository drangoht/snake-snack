# Pitfalls — Fonts and text


**⚠ Unity's fallback for missing glyphs exists ONLY on the desktop.** [inherited]
With a dynamic font, `Text` (uGUI) goes looking in the **system fonts** for what the font does not
contain: arrows `← → ↑ ↓` come out correctly under Windows with a font containing **none** of them. A
browser offers no system font: the **WebGL build loses them silently** — no white box, no warning,
the text simply closes over the void. Observed on Smily Volley: truncated help banners, invisible
scroll indicators.

The fallback declared at import (`fallbackFontReferences` → `LegacyRuntime.ttf`, set by script on the
`TrueTypeFontImporter`) **changes nothing**: tried, rebuilt, the arrows stayed missing.

**What works**: write only characters the font contains ("Up/Down" rather than "↑ ↓") and **draw the
symbols as sprites**. Check the `cmap` table before trusting anything — a 20-line Python script reads
it and answers yes or no. And check it **in the browser**, not by reasoning.

**Free fonts**: take the `.ttf` **and its `OFL.txt`** from the `google/fonts` repository (SIL OFL):
`https://raw.githubusercontent.com/google/fonts/main/ofl/<family>/<File>.ttf`.
⚠ Many families now only exist in a **variable version** (`Fredoka[wdth,wght].ttf`): list the folder
before guessing the URL (`https://api.github.com/repos/google/fonts/contents/ofl/<family>`). ⚠ The
`fonts.googleapis.com/css` API returns a URL whose file **is not a valid TTF** (signature `f89b`): a
real TTF starts with `00 01 00 00`, and a 39 KB file containing HTML is a disguised 404 page.

**⚠ "The family exists on Google Fonts" does not mean "a static weight exists".** Observed on
**Nunito** on 2026-08-28, after the same pitfall had ruled out Fredoka: `ofl/nunito` contains only
`Nunito[wght].ttf` and `Nunito-Italic[wght].ttf`, no `static/` folder. Nothing reports it — the family
displays normally on fonts.google.com (the site serves the variable file), and a guessed
`static/Nunito-SemiBold.ttf` URL returns a 39 KB 404 page that looks like a downloaded file.

**What settles it in one more request**: `ofl/<family>/upstream_info.md` quotes upstream's
`sources/config.yaml`. `buildStatic: false` there means the static weights are **not missing through
a publishing delay, but never built** — no point looking elsewhere, hunting for a release, or
waiting. Check both: the folder listing says the state, `upstream_info.md` says whether it will
change.

**What works, and does not force a change of family**: **instantiate** the variable file at a fixed
weight (`fontTools.varLib.instancer`, `updateFontNames=True`). The product is an **ordinary static**
`.ttf` — Unity imports it as a `TrueTypeFontImporter` like any other, and the ban on "a variable file
imported as though it fixed a weight" does not apply: the weight is frozen in the file, not chosen at
runtime. Done here by `tools/generate_fonts.py` (Nunito 600 / 800).
⚠ Two checks that raise nothing if skipped: the **`cmap` table of the instantiated file** (that is
the one imported, not upstream), and the **Reserved Font Name** in the `OFL.txt` — if there is one,
the modified version can **not** keep the family name, and nothing in the tooling will say so.
