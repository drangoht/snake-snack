<#
.SYNOPSIS
    Verifie les outils dont le projet a besoin et memorise leurs chemins.

.DESCRIPTION
    A lancer une fois apres un clone, ou le jour ou un script annonce « Unity introuvable ». Ce
    qu'il trouve est ecrit dans tools/local.settings.json, qui n'est PAS versionne : chaque poste a
    ses propres chemins, et un chemin d'un autre poste committe ici casserait la machine suivante.

.PARAMETER UnityPath
    Chemin d'Unity.exe (l'exe, son dossier `Editor`, ou le dossier de version : les trois marchent).
    Unity Hub > Installs > roue dentee > « Show in Explorer » donne le dossier exact.

.PARAMETER Python
    Chemin ou nom de l'interpreteur Python, si `py` n'est pas dans le PATH.

.EXAMPLE
    & "tools/configurer.ps1"
    & "tools/configurer.ps1" -UnityPath "D:\Unity\6000.5.6f1\Editor\Unity.exe"
#>

param(
    [string]$UnityPath = "",
    [string]$Python = ""
)

$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environnement.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$manquants = @()

Write-Host "Environnement du projet" -ForegroundColor Cyan
Write-Host "  racine  : $ProjectRoot" -ForegroundColor DarkGray

# --- Unity ---------------------------------------------------------------------------
$version = Get-VersionProjet
if ($version) { Write-Host "  version : Unity $version (ProjectSettings/ProjectVersion.txt)" -ForegroundColor DarkGray }

$unity = Resolve-UnityPath -UnityPath $UnityPath -Memoriser -Silencieux
if ($unity) {
    Write-Host "  Unity   : $unity" -ForegroundColor DarkGray
} else {
    $manquants += "Unity"
    Write-Host "  Unity   : INTROUVABLE" -ForegroundColor Red
    $installees = Find-UnityInstallations
    if ($installees.Count -gt 0) {
        Write-Host "    versions detectees :" -ForegroundColor DarkGray
        $installees | ForEach-Object { Write-Host "      $($_.Version)  $($_.Chemin)" -ForegroundColor DarkGray }
    } else {
        Write-Host "    aucune installation dans les emplacements d'Unity Hub :" -ForegroundColor DarkGray
        Get-RacinesUnity | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
        Write-Host "    -> relancer avec -UnityPath `"<chemin>\Unity.exe`"" -ForegroundColor Yellow
    }
}

# --- Python (pilotage du jeu, serveur web local) --------------------------------------
$py = Resolve-PythonCommand -Python $Python -Memoriser
if ($py) {
    Write-Host "  Python  : $py" -ForegroundColor DarkGray
} else {
    $manquants += "Python"
    Write-Host "  Python  : introuvable - piloter_jeu.py et serve_web.py ne pourront pas tourner" -ForegroundColor Yellow
    Write-Host "    -> installer Python 3, ou relancer avec -Python `"<chemin>\python.exe`"" -ForegroundColor DarkGray
}

# --- .NET (tests des regles, sans moteur) ---------------------------------------------
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if ($dotnet) {
    Write-Host "  dotnet  : $($dotnet.Source)" -ForegroundColor DarkGray
} else {
    Write-Host "  dotnet  : introuvable - 'dotnet test' et le hook de tests ne tourneront pas" -ForegroundColor Yellow
    Write-Host "    -> installer le SDK .NET 8" -ForegroundColor DarkGray
}

# --- Butler (publication itch.io) -----------------------------------------------------
# Fourni et tenu a jour par l'app itch.io ; on ne l'installe pas soi-meme.
$brothGlob = Join-Path $env:APPDATA "itch\broth\butler\versions\*\butler.exe"
$butler = Get-ChildItem -Path $brothGlob -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($butler) {
    Write-Host "  butler  : $($butler.FullName)" -ForegroundColor DarkGray
} else {
    Write-Host "  butler  : absent - necessaire seulement pour publier" -ForegroundColor DarkGray
    Write-Host "    -> lancer l'app itch.io une fois, elle l'installe et le met a jour" -ForegroundColor DarkGray
}

# --- Etat du projet Unity --------------------------------------------------------------
Write-Host ""
if (Test-Path (Join-Path $ProjectRoot "Library")) {
    Write-Host "Projet deja importe (Library/ present)." -ForegroundColor Green
    Write-Host "  Construire et regarder :  & `"tools/build.ps1`" -Lancer" -ForegroundColor DarkGray
} else {
    Write-Host "Projet jamais importe : le premier build s'en charge (~20 min)." -ForegroundColor Cyan
    Write-Host "  Rien a ouvrir dans Unity Hub -- l'import se fait en batchmode." -ForegroundColor DarkGray
    Write-Host "  & `"tools/build.ps1`" -Lancer" -ForegroundColor DarkGray
}

if ($manquants.Count -gt 0) {
    Write-Host ""
    Write-Host "Manque encore : $($manquants -join ', ')" -ForegroundColor Yellow
    exit 1
}
