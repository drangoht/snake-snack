<#
.SYNOPSIS
    Construit le jeu en ligne de commande, sans ouvrir l'editeur.

.DESCRIPTION
    Point d'entree unique de la chaine de build : c'est cette commande que citent le README, les
    agents et les skills. Personne n'ecrit plus le chemin d'Unity a la main -- il est resolu par
    tools/environnement.ps1 puis memorise.

    Le script refuse de partir si l'editeur est ouvert, exige la reussite ANNONCEE par BuildTools
    dans le journal (un code retour nul ne distingue pas « construit » de « rien a faire »), puis
    affiche le tampon du binaire produit.

.PARAMETER Target
    `windows` (defaut), `web`, ou `tout` pour enchainer les deux.

.PARAMETER UnityPath
    Chemin d'Unity.exe. Inutile apres la premiere fois : il est memorise dans
    tools/local.settings.json (non versionne).

.PARAMETER Lancer
    Enchaine sur tools/piloter_jeu.py : lance le build Windows et capture l'ecran. C'est la
    difference entre « ca compile » et « je l'ai vu tourner ».

.PARAMETER Methode
    Appelle une methode d'editeur au lieu de construire -- typiquement un fichier JETABLE depose
    dans Assets/Editor pour mesurer une geometrie qu'on ne peut pas prouver en jouant. Evite d'aller
    rechercher le chemin d'Unity pour ce seul usage.

.EXAMPLE
    & "tools/build.ps1"
    & "tools/build.ps1" -Target web
    & "tools/build.ps1" -Lancer -Capture docs/verif.png
    & "tools/build.ps1" -Methode SnakeSnack.EditorTools.Mesures.Verifier
    & "tools/build.ps1" -UnityPath "<dossier-unity>\<version>\Editor\Unity.exe"
#>

param(
    [ValidateSet("windows", "web", "tout")][string]$Target = "windows",
    [string]$UnityPath = "",
    [string]$Methode = "",
    [switch]$Lancer,
    [string]$Capture = "",
    [switch]$Silencieux
)

# NB : PAS "Stop". Unity ecrit sa progression sur stderr, ce que PowerShell 5.1 prend pour une
# erreur. Seul le code retour du processus fait foi.
$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environnement.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

function Fail($msg) { Write-Host "ERREUR : $msg" -ForegroundColor Red; exit 1 }

$Unity = Get-UnityPathOuMourir -UnityPath $UnityPath -Memoriser -Silencieux:$Silencieux

# ⚠ Un build en ligne de commande echoue si l'editeur tient le verrou du projet (« another Unity
# instance is running »). Ne jamais tuer l'editeur : quelqu'un y travaille peut-etre.
if (Get-Process Unity -ErrorAction SilentlyContinue) {
    Fail "L'editeur Unity est ouvert : ferme-le, le build en ligne de commande ne peut pas prendre le verrou."
}

$dossierLogs = Join-Path $ProjectRoot "Logs"
if (-not (Test-Path $dossierLogs)) { New-Item -ItemType Directory -Path $dossierLogs -Force | Out-Null }

# --- Methode d'editeur ponctuelle -----------------------------------------------------
# Sortie anticipee : on n'est pas la pour construire, mais pour faire tourner un bout de code dans
# l'editeur (mesure, diagnostic) et lire son journal.
if ($Methode) {
    $log = Join-Path $dossierLogs "methode.log"
    Write-Host "Appel de $Methode (journal : Logs\methode.log)..." -ForegroundColor Yellow
    $proc = Start-Process -FilePath $Unity -PassThru -Wait -NoNewWindow -ArgumentList @(
        "-batchmode", "-quit",
        "-projectPath", $ProjectRoot,
        "-logFile", $log,
        "-executeMethod", $Methode
    )
    if (-not (Test-Path $log)) { Fail "Aucun journal ecrit ($log) : Unity n'a pas demarre." }
    if ($proc.ExitCode -ne 0)  { Fail "$Methode a echoue (code $($proc.ExitCode)) - voir $log" }
    Write-Host "$Methode OK - lire Logs\methode.log" -ForegroundColor Green
    exit 0
}

$cibles = if ($Target -eq "tout") { @("windows", "web") } else { @($Target) }

# Le premier build d'une plateforme importe tous les assets et compile les shaders : compter une
# vingtaine de minutes. C'est aussi ce qui genere ProjectSettings/ et Library/ sur un projet neuf
# -- il n'y a donc rien a ouvrir dans Unity Hub avant.
$premierLancement = -not (Test-Path (Join-Path $ProjectRoot "Library"))
if ($premierLancement) {
    Write-Host "Premier build : Unity importe tout le projet (~20 min) et genere Library/." -ForegroundColor Yellow
}

foreach ($cible in $cibles) {
    if ($cible -eq "web") {
        $methodeBuild = "SnakeSnack.EditorTools.BuildTools.RebuildWeb"
        $reussite = "Build web reussi"
        $dossier  = Join-Path $ProjectRoot "Build\Web"
    } else {
        $methodeBuild = "SnakeSnack.EditorTools.BuildTools.RebuildEverything"
        $reussite = "Build Windows reussi"
        $dossier  = Join-Path $ProjectRoot "Build\Windows"
    }

    $log = Join-Path $dossierLogs "build-$cible.log"
    Write-Host "Build $cible en cours (journal : Logs\build-$cible.log)..." -ForegroundColor Yellow

    # ⚠ Start-Process et non l'operateur d'appel `&` : lance par `&`, Unity rend la main
    # IMMEDIATEMENT sans rien faire, $LASTEXITCODE reste vide, et le script poursuit comme si tout
    # allait bien. Un lancement qui echoue en silence est pire qu'un lancement qui echoue.
    $proc = Start-Process -FilePath $Unity -PassThru -Wait -NoNewWindow -ArgumentList @(
        "-batchmode", "-quit",
        "-projectPath", $ProjectRoot,
        "-logFile", $log,
        "-executeMethod", $methodeBuild
    )

    if (-not (Test-Path $log))  { Fail "Aucun journal ecrit ($log) : Unity n'a pas demarre." }
    if ($proc.ExitCode -ne 0)   { Fail "Build $cible echoue (code $($proc.ExitCode)) - voir $log" }
    if (-not (Select-String -Path $log -Pattern $reussite -Quiet)) {
        Fail "Build $cible : aucune reussite confirmee dans $log"
    }

    # Le tampon dit QUOI vient d'etre construit. Ni la date d'un fichier (le build est incremental)
    # ni les metadonnees Windows (qui decrivent le moteur) ne le disent.
    $tampon = Join-Path $dossier "build_stamp.json"
    if (Test-Path $tampon) {
        $stamp = Get-Content $tampon -Raw | ConvertFrom-Json
        Write-Host "Build $cible OK : v$($stamp.version)-$($stamp.sha)  ->  $dossier" -ForegroundColor Green
    } else {
        Write-Host "Build $cible OK  ->  $dossier" -ForegroundColor Green
        Write-Host "AVERTISSEMENT : pas de build_stamp.json - le binaire ne porte pas son identite." -ForegroundColor Yellow
    }
}

# --- Constater, plutot que conclure --------------------------------------------------
if ($Lancer -or $Capture) {
    $python = Resolve-PythonCommand -Memoriser
    if (-not $python) {
        Write-Host "AVERTISSEMENT : Python introuvable, impossible de lancer le jeu automatiquement." -ForegroundColor Yellow
        Write-Host "  Installer Python 3, ou renseigner tools/local.settings.json : { `"python`": `"...`" }" -ForegroundColor DarkGray
        exit 0
    }
    if (-not $Capture) { $Capture = "docs\verif.png" }

    Write-Host "Lancement du jeu et capture ($Capture)..." -ForegroundColor Cyan
    & $python (Join-Path $PSScriptRoot "piloter_jeu.py") --lancer --attendre 4 --capture $Capture
    if ($LASTEXITCODE -ne 0) {
        Write-Host "AVERTISSEMENT : piloter_jeu.py a rendu $LASTEXITCODE - lire sa sortie ci-dessus." -ForegroundColor Yellow
    }
}

# --- Code retour explicite -----------------------------------------------------------
# ⚠ SANS CE `exit 0`, UN BUILD REUSSI EST LU COMME UN ECHEC PAR L'APPELANT.
# Ce script verifie la reussite par `$proc.ExitCode` et par la ligne de confirmation du journal --
# mais `Start-Process -PassThru -Wait` NE MET PAS A JOUR `$LASTEXITCODE`. Sans sortie explicite,
# `$LASTEXITCODE` garde la valeur du dernier executable natif appele avant (git, py...), et
# `release_itch.ps1`, qui teste `$LASTEXITCODE` apres `& build.ps1`, echoue sur un build parfait.
# Constate le 2026-08-28 : le journal disait « Build web OK : v0.1.0-891ab4c », la ligne suivante
# « ERREUR : Build web echoue ».
# Un avertissement de `piloter_jeu.py` ci-dessus ne remet pas le BUILD en cause : il a deja ete
# valide plus haut, et c'est le build que ce code retour annonce.
exit 0
