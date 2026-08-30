---
name: publier-itch
description: Publish a new version of Snake Snack on itch.io (Unity build → Butler push → commit of the version number). To be invoked when the user asks to "publish", "release", "push to itch", "ship a new version". Chains the build, the push and the commit through tools/release_itch.ps1.
---

# Publishing on itch.io — Snake Snack

Distribution: **itch.io + Butler** (`Drangoht/snake-snack`). A `butler push` gives auto-update to the
players of the itch app (wharf differential patch); web players are always up to date, the page serving
the current build. Detailed runbook: `docs/RELEASE.md`. The `release-manager` agent does the same thing
end to end, devlog included.

## Procedure (in order)

### 1. Choose the version number
Semantics `MAJOR.MINOR.PATCH`. The current version is `bundleVersion` in
`ProjectSettings/ProjectSettings.asset` — **do not edit it by hand**, the script sets it itself.
- **patch** (x.y.**Z**): bugfix, minor adjustment;
- **minor** (x.**Y**.0): new content, new mechanic;
- **major** (**X**.0.0): overhaul, breaking change.

If the nature is not obvious, propose the bump and carry on without blocking.

### 2. Commit everything BEFORE running the script
The build stamp (`v<version>-<sha>`) designates the published commit. A modified tree produces a stamp
suffixed with `+`, which matches **no commit** — the script reports it, it does not prevent it.

⚠ **`Assets/Scenes/Game.unity` comes out modified at every build** (`SceneBuilder` renumbers every
`fileID`: thousands of diff lines for an identical scene). Discard it with
`git checkout -- Assets/Scenes/Game.unity`, **unless `SceneBuilder.cs` changed**.

### 3. Dry run, then publication
From the root, **without `-ExecutionPolicy Bypass`** (that flag is refused by the automatic classifier
and makes the call fail):
```
& "tools/release_itch.ps1" -Version X.Y.Z -DryRun            # goes as far as staging, publishes nothing
& "tools/release_itch.ps1" -Version X.Y.Z                    # web channel (default)
& "tools/release_itch.ps1" -Version X.Y.Z -Target windows    # downloadable channel
```
Generous timeout: a WebGL build takes ten minutes or so.

The script chains: `bundleVersion` set → Unity build (regenerated scene included) → check that the
build **really** carries the requested version → clean staging → `butler push` → commit and push of the
version number.

Useful parameters: `-SkipBuild` (re-push of a build you have just made yourself — the script checks its
stamp all the same), `-Channel`, `-Itch user/slug`.

### 4. Verify
- Output: "Publish OK - version X.Y.Z pushed". The `butler status` table may show the old version as
  long as the build is "processing" — that is normal.
- Open the page and launch the game: the stamp at the bottom right must carry the published version.

## Prerequisites / pitfalls
- **The Unity editor must be CLOSED**, otherwise the command-line build fails.
- **Butler authenticated**: provided by the itch app (`broth` folder, auto-detected). If "not
  authorized", run `"<butler.exe>" login` once (path shown by the script).
- ⚠ **One release has already shipped the binary of the PREVIOUS version.** Hence the stamp check: the
  script requires `build_stamp.json` to carry the requested version. Do not bypass it.
- ⚠ **The build's date proves nothing**: Unity builds incrementally, an identical file is not
  rewritten. Only the embedded version settles it. (And the Windows metadata of a Unity `.exe` describe
  the **engine**, not the game.)
- ⚠ **Never test `$?` after a native exe in PowerShell 5.1**: `git`, Unity and Butler write their
  progress on stderr even when all is well. Only `$LASTEXITCODE` is authoritative.
- ⚠ **The channel's name decides whether the file is playable in the browser**: `html5` (or `html`, or
  `web`) is recognised as such, any other name produces an archive to download — which installs
  perfectly and does not play. Nothing reports it.

## Prerequisites on the itch.io side, to be done ONCE
- *Kind of project* = **HTML** (as long as the project is "Downloadable", the web build downloads
  instead of playing), and the file ticked **"This file will be played in the browser"**.
- The **Mobile friendly** box, the **Classification** tab, the declared **orientation**: these three
  settings are in no file of the repository and are checked by hand.

## After the release
Update `README.md` / `CLAUDE.md` and the store page's text (`docs/ITCH_STORE_PAGE.md`) if the version
changes something visible. Devlog: written in `docs/DEVLOG.md`, then pasted on itch — ⚠ **tick
"Published"**, without which the post stays a draft saying nothing about it.
