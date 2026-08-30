<#
.SYNOPSIS
    Publishes a version of the game on itch.io (web or Windows channel).

.DESCRIPTION
    Chains: version number written into the project -> Unity build (scene included) -> check of the
    stamp produced BY the build -> clean distribution folder -> `butler push` -> version.json
    manifest -> commit + push.

    Every guard rail in this script has been paid for at least once. The most important one:
    WE CHECK THAT WHAT WE PUSH IS WHAT WE HAVE JUST BUILT, because one release has already shipped
    the binary of the PREVIOUS version without a single error being raised.

.PARAMETER Version
    Number shown on itch (e.g. 1.0.0). Mandatory: nothing else in the repository declares it, and
    setting it HERE is the decision to publish.

.PARAMETER Target
    `web` (default) or `windows`. The two targets differ in five things only: the folder built, the
    editor method that produces it, what we require to find in it, what we copy and the itch channel.

.PARAMETER UnityPath
    Path to Unity.exe. Not needed after the first time: it is resolved by tools/environment.ps1 then
    remembered in tools/local.settings.json.

.PARAMETER SkipBuild
    Reuses the build folder already present. Only to be used if you have just built it yourself: the
    script checks in any case that its stamp carries the requested version.

.PARAMETER DryRun
    Goes as far as the distribution folder and stops BEFORE butler and before any commit. It is the
    only way to exercise the chain without publishing: a release script that can only be tried by
    publishing is only ever tested in production.

.EXAMPLE
    & "tools/release_itch.ps1" -Version 1.0.0 -DryRun
    & "tools/release_itch.ps1" -Version 1.0.0
    & "tools/release_itch.ps1" -Version 1.0.0 -Target windows
#>

param(
    [Parameter(Mandatory = $true)][string]$Version,
    [ValidateSet("web", "windows")][string]$Target = "web",
    [string]$Itch = "Drangoht/snake-snack",
    [string]$Channel = "",
    [string]$UnityPath = "",
    [switch]$SkipBuild,
    [switch]$DryRun
)

# NB: NOT "Stop". Unity, git and butler write their progress on stderr, which PowerShell 5.1 takes
# for an error. Only $LASTEXITCODE is authoritative after a native executable.
$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environment.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Settings    = Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset"

function Fail($msg) { Write-Host "ERROR: $msg" -ForegroundColor Red; exit 1 }

# --- Target ------------------------------------------------------------------------
#
# ⚠ The channel name decides, on the itch.io side, whether the file is PLAYABLE IN THE BROWSER: a
# channel named `html5` (or `html`, or `web`) is recognised as such, any other name produces an
# archive to download. A web build pushed to a badly named channel installs perfectly -- and does not
# play. Nothing reports it.
if ($Target -eq "web") {
    $BuildDir       = Join-Path $ProjectRoot "Build\Web"
    $DefaultChannel = "html5"
    # index.html: the page itself. Build\: the wasm, the data and the loader.
    # build_stamp.json: the identity card of what has just been built.
    $Required       = @("index.html", "Build", "build_stamp.json")
} else {
    $BuildDir       = Join-Path $ProjectRoot "Build\Windows"
    $DefaultChannel = "windows"
    $Required       = @("SnakeSnack.exe", "SnakeSnack_Data", "UnityPlayer.dll", "build_stamp.json")
}

if (-not $Channel) { $Channel = $DefaultChannel }
$Staging = Join-Path $ProjectRoot "Build\staging-$Target"

# Unity is resolved NOW, before touching anything: finding out it is missing after writing
# bundleVersion would leave the repository modified for nothing.
if (-not $SkipBuild) { $null = Get-UnityPathOrDie -UnityPath $UnityPath -Remember -Quiet }
if (-not (Test-Path $Settings)) { Fail "ProjectSettings.asset missing: run `"tools/build.ps1`" once (it imports the project) before publishing." }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "Version expected in x.y.z format (received: $Version)" }

# --- Butler ------------------------------------------------------------------------
# Supplied by the itch.io app (broth folder), which keeps it up to date on its own.
$brothGlob = Join-Path $env:APPDATA "itch\broth\butler\versions\*\butler.exe"
$butler = Get-ChildItem -Path $brothGlob -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $butler) {
    Fail "butler.exe not found. Run the itch.io app once, or install butler from https://itchio.itch.io/butler"
}
$Butler = $butler.FullName

Write-Host "Butler  : $Butler" -ForegroundColor Cyan
Write-Host "Target  : $Target" -ForegroundColor Cyan
Write-Host "Version : $Version  ->  $Itch`:$Channel" -ForegroundColor Cyan

# --- 1. Version in the project settings --------------------------------------------
# This is what Application.version reads, hence the stamp shown in game AND the comparison with the
# manifest. Leaving it behind would make the binary announce itself under an old number.
$content = Get-Content $Settings -Raw
$content = $content -replace '(?m)^(\s*bundleVersion:\s*).*$', "`${1}$Version"
Set-Content -Path $Settings -Value $content -Encoding utf8 -NoNewline
Write-Host "bundleVersion set to $Version." -ForegroundColor DarkGray

# --- 2. Build ----------------------------------------------------------------------
if (-not $SkipBuild) {
    # A single build path in the whole repository: build.ps1 resolves Unity, refuses to start if the
    # editor is open, and requires the success ANNOUNCED by BuildTools in the log.
    & (Join-Path $PSScriptRoot "build.ps1") -Target $Target -UnityPath $UnityPath
    if ($LASTEXITCODE -ne 0) { Fail "$Target build failed - see Logs\build-$Target.log" }
} else {
    Write-Host "SkipBuild: reusing the existing folder." -ForegroundColor DarkGray
    if (-not (Test-Path $BuildDir)) { Fail "SkipBuild requested but no build: $BuildDir" }
}

# --- 3. Build verification ---------------------------------------------------------
# What ships must contain enough to run. An incomplete data folder only shows at launch, that is, on
# the player's machine.
foreach ($required in $Required) {
    if (-not (Test-Path (Join-Path $BuildDir $required))) { Fail "Missing item in the build: $required" }
}

# The stamp produced BY the build: the last point at which we can notice we are about to publish
# something other than what we think.
# ⚠ The DATE proves nothing: the Unity build is incremental, an identical file is not rewritten.
# ⚠ The Windows metadata of a Unity .exe describes the ENGINE ("6000.x"), not the game. Only the
# embedded version, written just before the build, settles it.
$stamp = Get-Content (Join-Path $BuildDir "build_stamp.json") -Raw | ConvertFrom-Json
if ($stamp.version -ne $Version) {
    Fail "The build carries version '$($stamp.version)' while we are publishing '$Version' - stale build."
}
Write-Host "Build verified: v$($stamp.version)-$($stamp.sha) (built on $($stamp.date))." -ForegroundColor DarkGray

# The "+" suffix says the working tree carried changes: the build then matches NO COMMIT, and the
# stamp shown in game will not allow a bug report to be replayed.
if ($stamp.sha -like "*+") {
    Write-Host "WARNING: build from a modified tree ($($stamp.sha)) - it matches no commit." -ForegroundColor Yellow
} elseif ($stamp.sha -eq "dev") {
    Write-Host "WARNING: the build could not read git - the stamp will say 'dev' to players." -ForegroundColor Yellow
}

# --- 4. Clean distribution folder --------------------------------------------------
# Butler diffs file by file: we push a FOLDER, without the artefacts the build drops next to it
# (Burst symbols, which Unity itself names "DoNotShip").
if (Test-Path $Staging) { Remove-Item $Staging -Recurse -Force }
New-Item -ItemType Directory -Path $Staging -Force | Out-Null

Copy-Item (Join-Path $BuildDir "*") -Destination $Staging -Recurse -Force -Exclude "*BurstDebugInformation*"
Get-ChildItem $Staging -Directory -Filter "*BurstDebugInformation*" |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$size = [math]::Round((Get-ChildItem $Staging -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "Staging ready: $Staging ($size MB)" -ForegroundColor Cyan

# --- 5. Butler push ----------------------------------------------------------------
if ($DryRun) {
    Write-Host "`nDRY RUN: everything is ready, nothing has been published." -ForegroundColor Green
    Write-Host "  build   : $BuildDir" -ForegroundColor DarkGray
    Write-Host "  staging : $Staging" -ForegroundColor DarkGray
    Write-Host "  stamp   : v$($stamp.version)-$($stamp.sha)" -ForegroundColor DarkGray
    Write-Host "Re-run without -DryRun to push to $Itch`:$Channel." -ForegroundColor Green
    exit 0
}

Write-Host "Pushing to itch.io..." -ForegroundColor Yellow
& $Butler push $Staging "$Itch`:$Channel" --userversion $Version
if ($LASTEXITCODE -ne 0) {
    Fail "butler push failed (code $LASTEXITCODE). If 'not authorized', run once: `"$Butler`" login"
}

# --- 6. Version manifest -----------------------------------------------------------
# Players who DOWNLOADED the game do not have the itch app's auto-update: an in-game banner can read
# this file from raw.githubusercontent and announce the new version to them.
#
# ⚠ The manifest describes the DOWNLOADABLE version: it belongs to the Windows target only. A web
# player is always up to date (the page serves the current build). Pushing it from a web release
# would announce to every Windows player an update that does not exist.
$toCommit = @("ProjectSettings/ProjectSettings.asset")
if ($Target -eq "web") {
    Write-Host "Manifest unchanged: a web release announces nothing to Windows players." -ForegroundColor DarkGray
} else {
    $parts = $Itch.Split("/")
    $manifest = [ordered]@{ version = $Version; url = "https://$($parts[0]).itch.io/$($parts[1])" }
    ($manifest | ConvertTo-Json) | Out-File -FilePath (Join-Path $ProjectRoot "version.json") -Encoding utf8
    $toCommit += "version.json"
}

# --- 7. Commit of the version number -----------------------------------------------
Push-Location $ProjectRoot
git add $toCommit
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    # The message must say what is REALLY committed: announcing a manifest that a web release does
    # not touch would make the history wrong exactly where people come to consult it.
    $what = if ($Target -eq "web") { "web channel" } else { "manifest + project version" }
    git commit -m "chore(release): $Version ($what)"
    # ⚠ Do NOT test $? after a native exe: git writes its progress on stderr even when all is well,
    # which sets $? to false while the exit code is 0.
    if ($LASTEXITCODE -eq 0) {
        git push
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WARNING: git push failed - push the version commit by hand." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "Nothing to commit (version number unchanged)." -ForegroundColor DarkGray
}
Pop-Location

# --- 8. Status ---------------------------------------------------------------------
& $Butler status $Itch
Write-Host "`nPublish OK - version $Version pushed to $Itch`:$Channel" -ForegroundColor Green

if ($Target -eq "web") {
    Write-Host "The page serves the new build as soon as itch has finished processing it." -ForegroundColor Green
    Write-Host "⚠ Prerequisite on the itch.io side, to be done ONCE: `"Kind of project`" = HTML," -ForegroundColor Yellow
    Write-Host "  and the file ticked `"This file will be played in the browser`"." -ForegroundColor Yellow
} else {
    Write-Host "Players using the itch.io app will get the update automatically." -ForegroundColor Green
}
