# Publishing runbook — Snake Snack

Short version: skill **`/publier-itch`**. End to end, devlog included: the **`release-manager`** agent.
This document is the detail, to be read when something falls outside the ordinary.

## What is published, and where

| itch channel | What it carries | How the player gets it |
|---|---|---|
| `html5` | `Build/Web` | The page serves the current build — always up to date |
| `windows` | `Build/Windows` | Auto-update through the itch app, or download |

⚠ **The channel's name decides whether the file is playable in the browser.** `html5`, `html` or `web`
are recognised as such; any other name produces an archive to download, which installs perfectly and
**does not play**. Nothing warns about it.

## Prerequisites, to be done ONCE

1. **Create the page** on `https://Drangoht.itch.io/snake-snack`.
2. **`Kind of project` = HTML** for a web game (as long as the project is "Downloadable", the web build
   downloads instead of playing), and tick **"This file will be played in the browser"** on the file.
3. **Butler authenticated**: install the itch.io app (it provides and updates `butler.exe` in its
   `broth` folder, which the script detects on its own). If "not authorized", run
   `"<butler.exe>" login` once.
4. Check the three settings that are **in no file of the repository**: the **Mobile friendly** box, the
   **Classification** tab (including the player count), the declared **orientation**.

## The procedure

```
1. Choose the semver          x.y.Z fix · x.Y.0 content · X.0.0 overhaul
2. Commit EVERYTHING          otherwise the stamp carries a "+" and designates no commit
3. Close the Unity editor     otherwise the command-line build fails
4. & "tools/release_itch.ps1" -Version X.Y.Z -DryRun
5. & "tools/release_itch.ps1" -Version X.Y.Z
6. Verify                     launch the published game, read the stamp at the bottom right
7. Update the docs            README, CLAUDE.md, docs/ITCH_STORE_PAGE.md if visible
8. Devlog                     docs/DEVLOG.md, then pasted on itch — TICK "Published"
```

From the root, **without `-ExecutionPolicy Bypass`**: that flag is refused by the automatic classifier
and makes the call fail.

## What the script does, and why each step exists

1. **Sets `bundleVersion`** in `ProjectSettings.asset`. That is what `Application.version` reads, hence
   the stamp shown in game. Leaving it behind would make the build announce itself under an old number.
2. **Builds** (regenerated scene included), and requires an **explicit success sentence** in the log: a
   zero exit code does not distinguish "built" from "nothing to do".
3. **Checks the stamp produced BY the build.** ⚠ One release has already shipped the binary of the
   **previous** version without a single error being raised. Neither the date (incremental build: an
   identical file is not rewritten) nor the Windows metadata (which describe the **engine**) make it
   possible to notice. Only the embedded version settles it. **Do not bypass this check.**
4. **Prepares a clean distribution folder**, without the Burst symbols that Unity itself names
   "DoNotShip". Butler diffs file by file: we push a folder, not an archive.
5. **`butler push`** with `--userversion`.
6. **Commit + push** of the version number (and of the manifest, for the Windows target only — a web
   player is always up to date, and pushing them a manifest would announce to Windows players an update
   that does not exist).

## Diagnosis

| Symptom | Cause |
|---|---|
| "Unity.exe … is not recognized" | Unity's path never given: `& "tools/configure.ps1" -UnityPath "…"` |
| "another Unity instance is running" | The editor is open. Do not kill it: wait. |
| Build "fails" but no log written | Unity launched with `&` instead of `Start-Process -Wait` |
| "The build carries version X" | Stale build: `-SkipBuild` on a folder that was not rebuilt |
| Stamp suffixed with `+` | The tree was modified at build time — commit first |
| Stamp reading `dev` | git unavailable during the build |
| "not authorized" (butler) | `"<butler.exe>" login`, once |
| The web build downloads instead of playing | Badly named channel, or `Kind of project` ≠ HTML |
| "git push fails" although all is well | A `$?` tested after a native exe. Only `$LASTEXITCODE` is authoritative. |

## After the release

- The `butler status` table may show the old version as long as the build is "processing" — that is
  normal.
- ⚠ `Assets/Scenes/Game.unity` comes out modified (every `fileID` renumbered): discard it, **unless
  `SceneBuilder.cs` changed**.
