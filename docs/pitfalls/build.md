# Pitfalls — Build


**⚠ Launching Unity with the `&` operator in PowerShell returns IMMEDIATELY without doing anything.**
[inherited] No log, empty `$LASTEXITCODE`, and the script carries on as if all were well. Use
`Start-Process -Wait`. *A launch that fails silently is worse than a launch that fails.*

**⚠ A zero return code does not tell "built" from "nothing to do".** Require an **explicit success
phrase** in the log (that is what `tools/build.ps1` does).

**⚠ Worse: Unity exits with return code 0 while the build has FAILED.** Observed on a Windows build
whose log says `Build Finished, Result: Failure` (6 errors) then, thirty lines further down,
`Exiting batchmode successfully now!` and a code of 0. A script trusting the return code packages and
publishes an incomplete build folder **with nothing to warn it**. The success phrase in the log is
the only reliable signal.

**⚠ The DATE of a build artefact proves nothing**: Unity builds incrementally, an identical file is
**not rewritten**. A timestamp older than the build is normal. The first freshness guard written on
that basis failed on perfectly valid builds.

**⚠ The Windows metadata of a Unity `.exe` describes the ENGINE** ("6000.5.6f1"), not the game. A
check comparing the release version to that metadata always fails.

**⚠ Only the EMBEDDED version settles it**, because it is written just before the build. Hence
`build_stamp.json`, written **by** the build: it cannot announce a version the build did not put
there. **A release has already shipped the binary of the previous version with no error raised** —
that check is what prevents it.

**⚠ A build stamp written by the PUBLISHING script outlives its release.** [inherited] Written only
at publishing time, the file then stays in place, and every later local build shows the SHA of the
last release. *A freshness guard that lies is worse than no guard, since it is trusted.* It is
therefore written by the **build** (`BuildTools.StampGitSha`) and ignored by git — it is an artefact,
not a source.

**⚠ The command-line build fails if the Unity editor is open** ("another Unity instance is running").
Check `Get-Process Unity` or `Temp\UnityLockfile` — and **never kill the editor**: wait, or work on a
copy of `Assets` + `Packages` + `ProjectSettings`.

**⚠ The first build of a platform reimports every asset** (several tens of minutes); the later ones
are quick. Plan the timeout accordingly.

**⚠ The regenerated scene produces a huge, meaningless diff.** `SceneBuilder` renumbers every
`fileID`: thousands of lines added and as many removed for an identical scene. Discard it
(`git checkout --`) **unless `SceneBuilder.cs` has changed**. Without the matching exclusion in
`BuildTools.HasLocalChanges`, every build would declare itself built from a modified tree.


**⚠ `Start-Process -PassThru -Wait` DOES NOT UPDATE `$LASTEXITCODE`.** Observed on 2026-08-28, on the
first publishing attempt. `build.ps1` launches Unity through `Start-Process` (mandatory: launched
with `&`, Unity returns immediately without doing anything) and correctly checks `$proc.ExitCode`.
But it ended with **no explicit `exit`**: on leaving the script, `$LASTEXITCODE` still held the code
of the last *native* executable called before — git, `py`... — and `release_itch.ps1`, which tests
`$LASTEXITCODE` after `& build.ps1`, refused to publish a perfectly valid build. The two lines
followed each other in the same output:

```
web build OK: v0.1.0-891ab4c  ->  C:\CODE\JEUX\snake-snack\Build\Web
ERROR: web build failed - see Logs\build-web.log
```

The log it pointed at contained no error at all, which is the costly part: you go looking for the
defect in the build. Countermeasure: **every PowerShell script whose return code another one reads
ends with an explicit `exit`.** Never infer anything from a `$LASTEXITCODE` no native command has
set.
