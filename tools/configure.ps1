<#
.SYNOPSIS
    Checks the tools the project needs and remembers their paths.

.DESCRIPTION
    To run once after a clone, or the day a script announces "Unity not found". What it finds is
    written into tools/local.settings.json, which is NOT versioned: every machine has its own paths,
    and one machine's path committed here would break the next one.

.PARAMETER UnityPath
    Path to Unity.exe (the exe, its `Editor` folder, or the version folder: all three work).
    Unity Hub > Installs > cog > "Show in Explorer" gives the exact folder.

.PARAMETER Python
    Path or name of the Python interpreter, if `py` is not on PATH.

.EXAMPLE
    & "tools/configure.ps1"
    & "tools/configure.ps1" -UnityPath "D:\Unity\6000.5.6f1\Editor\Unity.exe"
#>

param(
    [string]$UnityPath = "",
    [string]$Python = ""
)

$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environment.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$missing = @()

Write-Host "Project environment" -ForegroundColor Cyan
Write-Host "  root    : $ProjectRoot" -ForegroundColor DarkGray

# --- Unity ---------------------------------------------------------------------------
$version = Get-ProjectVersion
if ($version) { Write-Host "  version : Unity $version (ProjectSettings/ProjectVersion.txt)" -ForegroundColor DarkGray }

$unity = Resolve-UnityPath -UnityPath $UnityPath -Remember -Quiet
if ($unity) {
    Write-Host "  Unity   : $unity" -ForegroundColor DarkGray
} else {
    $missing += "Unity"
    Write-Host "  Unity   : NOT FOUND" -ForegroundColor Red
    $installed = Find-UnityInstallations
    if ($installed.Count -gt 0) {
        Write-Host "    versions detected:" -ForegroundColor DarkGray
        $installed | ForEach-Object { Write-Host "      $($_.Version)  $($_.Path)" -ForegroundColor DarkGray }
    } else {
        Write-Host "    no installation in the Unity Hub locations:" -ForegroundColor DarkGray
        Get-UnityRoots | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
        Write-Host "    -> re-run with -UnityPath `"<path>\Unity.exe`"" -ForegroundColor Yellow
    }
}

# --- Python (driving the game, local web server) --------------------------------------
$py = Resolve-PythonCommand -Python $Python -Remember
if ($py) {
    Write-Host "  Python  : $py" -ForegroundColor DarkGray
} else {
    $missing += "Python"
    Write-Host "  Python  : not found - drive_game.py and serve_web.py will not run" -ForegroundColor Yellow
    Write-Host "    -> install Python 3, or re-run with -Python `"<path>\python.exe`"" -ForegroundColor DarkGray
}

# --- .NET (rule tests, engine-free) ----------------------------------------------------
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if ($dotnet) {
    Write-Host "  dotnet  : $($dotnet.Source)" -ForegroundColor DarkGray
} else {
    Write-Host "  dotnet  : not found - 'dotnet test' and the test hook will not run" -ForegroundColor Yellow
    Write-Host "    -> install the .NET 8 SDK" -ForegroundColor DarkGray
}

# --- Butler (itch.io publishing) ------------------------------------------------------
# Supplied and kept up to date by the itch.io app; we do not install it ourselves.
$brothGlob = Join-Path $env:APPDATA "itch\broth\butler\versions\*\butler.exe"
$butler = Get-ChildItem -Path $brothGlob -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($butler) {
    Write-Host "  butler  : $($butler.FullName)" -ForegroundColor DarkGray
} else {
    Write-Host "  butler  : absent - only needed to publish" -ForegroundColor DarkGray
    Write-Host "    -> run the itch.io app once, it installs and updates it" -ForegroundColor DarkGray
}

# --- State of the Unity project --------------------------------------------------------
Write-Host ""
if (Test-Path (Join-Path $ProjectRoot "Library")) {
    Write-Host "Project already imported (Library/ present)." -ForegroundColor Green
    Write-Host "  Build and watch:  & `"tools/build.ps1`" -Run" -ForegroundColor DarkGray
} else {
    Write-Host "Project never imported: the first build takes care of it (~20 min)." -ForegroundColor Cyan
    Write-Host "  Nothing to open in Unity Hub -- the import happens in batchmode." -ForegroundColor DarkGray
    Write-Host "  & `"tools/build.ps1`" -Run" -ForegroundColor DarkGray
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Still missing: $($missing -join ', ')" -ForegroundColor Yellow
    exit 1
}
