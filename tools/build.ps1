<#
.SYNOPSIS
    Builds the game from the command line, without opening the editor.

.DESCRIPTION
    The single entry point of the build chain: this is the command the README, the agents and the
    skills all quote. Nobody writes Unity's path by hand any more -- it is resolved by
    tools/environment.ps1 then remembered.

    The script refuses to start if the editor is open, requires the success ANNOUNCED by BuildTools
    in the log (a zero exit code does not tell "built" from "nothing to do"), then prints the stamp
    of the binary produced.

.PARAMETER Target
    `windows` (default), `web`, or `all` to chain both.

.PARAMETER UnityPath
    Path to Unity.exe. Not needed after the first time: it is remembered in
    tools/local.settings.json (not versioned).

.PARAMETER Run
    Chains on to tools/drive_game.py: launches the Windows build and captures the screen. That is the
    difference between "it compiles" and "I saw it run".

.PARAMETER Method
    Calls an editor method instead of building -- typically a THROWAWAY file dropped into
    Assets/Editor to measure a geometry that cannot be proven by playing. Saves looking up Unity's
    path for that single use.

.EXAMPLE
    & "tools/build.ps1"
    & "tools/build.ps1" -Target web
    & "tools/build.ps1" -Run -Capture docs/check.png
    & "tools/build.ps1" -Method SnakeSnack.EditorTools.Measures.Verify
    & "tools/build.ps1" -UnityPath "<unity-folder>\<version>\Editor\Unity.exe"
#>

param(
    [ValidateSet("windows", "web", "all")][string]$Target = "windows",
    [string]$UnityPath = "",
    [string]$Method = "",
    [switch]$Run,
    [string]$Capture = "",
    [switch]$Quiet
)

# NB: NOT "Stop". Unity writes its progress on stderr, which PowerShell 5.1 takes for an error. Only
# the process exit code is authoritative.
$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environment.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

function Fail($msg) { Write-Host "ERROR: $msg" -ForegroundColor Red; exit 1 }

$Unity = Get-UnityPathOrDie -UnityPath $UnityPath -Remember -Quiet:$Quiet

# ⚠ A command-line build fails if the editor holds the project lock ("another Unity instance is
# running"). Never kill the editor: somebody may be working in it.
if (Get-Process Unity -ErrorAction SilentlyContinue) {
    Fail "The Unity editor is open: close it, the command-line build cannot take the lock."
}

$logsFolder = Join-Path $ProjectRoot "Logs"
if (-not (Test-Path $logsFolder)) { New-Item -ItemType Directory -Path $logsFolder -Force | Out-Null }

# --- One-off editor method ------------------------------------------------------------
# Early exit: we are not here to build, but to run a piece of code inside the editor (measurement,
# diagnosis) and read its log.
if ($Method) {
    $log = Join-Path $logsFolder "method.log"
    Write-Host "Calling $Method (log: Logs\method.log)..." -ForegroundColor Yellow
    $proc = Start-Process -FilePath $Unity -PassThru -Wait -NoNewWindow -ArgumentList @(
        "-batchmode", "-quit",
        "-projectPath", $ProjectRoot,
        "-logFile", $log,
        "-executeMethod", $Method
    )
    if (-not (Test-Path $log)) { Fail "No log written ($log): Unity did not start." }
    if ($proc.ExitCode -ne 0)  { Fail "$Method failed (code $($proc.ExitCode)) - see $log" }
    Write-Host "$Method OK - read Logs\method.log" -ForegroundColor Green
    exit 0
}

$targets = if ($Target -eq "all") { @("windows", "web") } else { @($Target) }

# The first build of a platform imports every asset and compiles the shaders: allow about twenty
# minutes. It is also what generates ProjectSettings/ and Library/ on a fresh project -- so there is
# nothing to open in Unity Hub beforehand.
$firstRun = -not (Test-Path (Join-Path $ProjectRoot "Library"))
if ($firstRun) {
    Write-Host "First build: Unity imports the whole project (~20 min) and generates Library/." -ForegroundColor Yellow
}

foreach ($target in $targets) {
    if ($target -eq "web") {
        $buildMethod = "SnakeSnack.EditorTools.BuildTools.RebuildWeb"
        # ⚠ CONTRACT WITH Assets/Editor/BuildTools.cs: this phrase is searched for in the log.
        # Changing the wording there means changing it here, in the same commit.
        $success = "Web build succeeded"
        $folder  = Join-Path $ProjectRoot "Build\Web"
    } else {
        $buildMethod = "SnakeSnack.EditorTools.BuildTools.RebuildEverything"
        $success = "Windows build succeeded"
        $folder  = Join-Path $ProjectRoot "Build\Windows"
    }

    $log = Join-Path $logsFolder "build-$target.log"
    Write-Host "Building $target (log: Logs\build-$target.log)..." -ForegroundColor Yellow

    # ⚠ Start-Process and not the call operator `&`: launched with `&`, Unity returns IMMEDIATELY
    # without doing anything, $LASTEXITCODE stays empty, and the script carries on as if all were
    # well. A launch that fails silently is worse than a launch that fails.
    $proc = Start-Process -FilePath $Unity -PassThru -Wait -NoNewWindow -ArgumentList @(
        "-batchmode", "-quit",
        "-projectPath", $ProjectRoot,
        "-logFile", $log,
        "-executeMethod", $buildMethod
    )

    if (-not (Test-Path $log))  { Fail "No log written ($log): Unity did not start." }
    if ($proc.ExitCode -ne 0)   { Fail "$target build failed (code $($proc.ExitCode)) - see $log" }
    if (-not (Select-String -Path $log -Pattern $success -Quiet)) {
        Fail "$target build: no success confirmed in $log"
    }

    # The stamp says WHAT has just been built. Neither a file date (the build is incremental) nor
    # Windows metadata (which describes the engine) says it.
    $stampPath = Join-Path $folder "build_stamp.json"
    if (Test-Path $stampPath) {
        $stamp = Get-Content $stampPath -Raw | ConvertFrom-Json
        Write-Host "$target build OK: v$($stamp.version)-$($stamp.sha)  ->  $folder" -ForegroundColor Green
    } else {
        Write-Host "$target build OK  ->  $folder" -ForegroundColor Green
        Write-Host "WARNING: no build_stamp.json - the binary does not carry its identity." -ForegroundColor Yellow
    }
}

# --- Observe, rather than conclude ---------------------------------------------------
if ($Run -or $Capture) {
    $python = Resolve-PythonCommand -Remember
    if (-not $python) {
        Write-Host "WARNING: Python not found, cannot launch the game automatically." -ForegroundColor Yellow
        Write-Host "  Install Python 3, or fill in tools/local.settings.json: { `"python`": `"...`" }" -ForegroundColor DarkGray
        exit 0
    }
    if (-not $Capture) { $Capture = "docs\check.png" }

    Write-Host "Launching the game and capturing ($Capture)..." -ForegroundColor Cyan
    & $python (Join-Path $PSScriptRoot "drive_game.py") --launch --wait 4 --capture $Capture
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: drive_game.py returned $LASTEXITCODE - read its output above." -ForegroundColor Yellow
    }
}

# --- Explicit exit code --------------------------------------------------------------
# ⚠ WITHOUT THIS `exit 0`, A SUCCESSFUL BUILD IS READ AS A FAILURE BY THE CALLER.
# This script checks success through `$proc.ExitCode` and through the confirmation line in the log --
# but `Start-Process -PassThru -Wait` DOES NOT UPDATE `$LASTEXITCODE`. Without an explicit exit,
# `$LASTEXITCODE` keeps the value of the last native executable called before (git, py...), and
# `release_itch.ps1`, which tests `$LASTEXITCODE` after `& build.ps1`, fails on a perfect build.
# Observed on 2026-08-28: the log said "web build OK: v0.1.0-891ab4c", the next line said
# "ERROR: web build failed".
# A warning from `drive_game.py` above does not call the BUILD into question: it has already been
# validated further up, and it is the build that this exit code announces.
exit 0
