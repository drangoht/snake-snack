<#
.SYNOPSIS
    Publie une version du jeu sur itch.io (canal web ou Windows).

.DESCRIPTION
    Enchaine : numero de version pose dans le projet -> build Unity (scene comprise) -> verification
    du tampon produit PAR le build -> dossier de distribution propre -> `butler push` -> manifeste
    version.json -> commit + push.

    Chaque garde-fou de ce script a ete paye au moins une fois. Le plus important :
    ON VERIFIE QUE CE QU'ON POUSSE EST BIEN CE QU'ON VIENT DE CONSTRUIRE, parce qu'une release a
    deja expedie le binaire de la version PRECEDENTE sans qu'aucune erreur ne soit levee.

.PARAMETER Version
    Numero affiche sur itch (ex. 1.0.0). Obligatoire : rien ne le declare ailleurs dans le depot,
    le poser ICI est la decision de publier.

.PARAMETER Target
    `web` (defaut) ou `windows`. Les deux cibles ne different que par cinq choses : le dossier
    construit, la methode d'editeur qui le produit, ce qu'on exige d'y trouver, ce qu'on copie et
    le canal itch.

.PARAMETER UnityPath
    Chemin d'Unity.exe. Inutile apres la premiere fois : il est resolu par tools/environnement.ps1
    puis memorise dans tools/local.settings.json.

.PARAMETER SkipBuild
    Reutilise le dossier de build deja present. A n'employer que si l'on vient de le construire
    soi-meme : le script verifie de toute facon que son tampon porte la version demandee.

.PARAMETER DryRun
    Va jusqu'au dossier de distribution et s'arrete AVANT butler et avant tout commit. C'est le seul
    moyen d'eprouver la chaine sans publier : un script de release qu'on ne peut essayer qu'en
    publiant ne se teste jamais qu'en production.

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

# NB : PAS "Stop". Unity, git et butler ecrivent leur progression sur stderr, ce que PowerShell 5.1
# prend pour une erreur. Seul $LASTEXITCODE fait foi apres un executable natif.
$ErrorActionPreference = "Continue"

. "$PSScriptRoot\environnement.ps1"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Settings    = Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset"

function Fail($msg) { Write-Host "ERREUR : $msg" -ForegroundColor Red; exit 1 }

# --- Cible -------------------------------------------------------------------------
#
# ⚠ Le nom du canal decide, cote itch.io, si le fichier est JOUABLE DANS LE NAVIGATEUR : un canal
# nomme `html5` (ou `html`, ou `web`) est reconnu comme tel, n'importe quel autre nom produit une
# archive a telecharger. Un build web pousse sur un canal mal nomme s'installe parfaitement -- et ne
# se joue pas. Rien ne le signale.
if ($Target -eq "web") {
    $BuildDir       = Join-Path $ProjectRoot "Build\Web"
    $DefaultChannel = "html5"
    # index.html : la page elle-meme. Build\ : le wasm, les donnees et le chargeur.
    # build_stamp.json : la carte d'identite de ce qui vient d'etre construit.
    $Required       = @("index.html", "Build", "build_stamp.json")
} else {
    $BuildDir       = Join-Path $ProjectRoot "Build\Windows"
    $DefaultChannel = "windows"
    $Required       = @("SnakeSnack.exe", "SnakeSnack_Data", "UnityPlayer.dll", "build_stamp.json")
}

if (-not $Channel) { $Channel = $DefaultChannel }
$Staging = Join-Path $ProjectRoot "Build\staging-$Target"

# Unity est resolu MAINTENANT, avant de toucher a quoi que ce soit : decouvrir qu'il manque apres
# avoir pose bundleVersion laisserait le depot modifie pour rien.
if (-not $SkipBuild) { $null = Get-UnityPathOuMourir -UnityPath $UnityPath -Memoriser -Silencieux }
if (-not (Test-Path $Settings)) { Fail "ProjectSettings.asset absent : lance une premiere fois `"tools/build.ps1`" (il importe le projet) avant de publier." }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "Version attendue au format x.y.z (recu : $Version)" }

# --- Butler ------------------------------------------------------------------------
# Fourni par l'app itch.io (dossier broth), qui le tient a jour toute seule.
$brothGlob = Join-Path $env:APPDATA "itch\broth\butler\versions\*\butler.exe"
$butler = Get-ChildItem -Path $brothGlob -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $butler) {
    Fail "butler.exe introuvable. Lance l'app itch.io une fois, ou installe butler depuis https://itchio.itch.io/butler"
}
$Butler = $butler.FullName

Write-Host "Butler  : $Butler" -ForegroundColor Cyan
Write-Host "Cible   : $Target" -ForegroundColor Cyan
Write-Host "Version : $Version  ->  $Itch`:$Channel" -ForegroundColor Cyan

# --- 1. Version dans les reglages du projet ----------------------------------------
# C'est elle que lit Application.version, donc le tampon affiche en jeu ET la comparaison avec le
# manifeste. La laisser derriere ferait s'annoncer le binaire sous un ancien numero.
$content = Get-Content $Settings -Raw
$content = $content -replace '(?m)^(\s*bundleVersion:\s*).*$', "`${1}$Version"
Set-Content -Path $Settings -Value $content -Encoding utf8 -NoNewline
Write-Host "bundleVersion pose a $Version." -ForegroundColor DarkGray

# --- 2. Build ----------------------------------------------------------------------
if (-not $SkipBuild) {
    # Un seul chemin de build dans tout le depot : build.ps1 resout Unity, refuse de partir si
    # l'editeur est ouvert, et exige la reussite ANNONCEE par BuildTools dans le journal.
    & (Join-Path $PSScriptRoot "build.ps1") -Target $Target -UnityPath $UnityPath
    if ($LASTEXITCODE -ne 0) { Fail "Build $Target echoue - voir Logs\build-$Target.log" }
} else {
    Write-Host "SkipBuild : dossier existant reutilise." -ForegroundColor DarkGray
    if (-not (Test-Path $BuildDir)) { Fail "SkipBuild demande mais aucun build : $BuildDir" }
}

# --- 3. Verification du build ------------------------------------------------------
# Ce qui part doit contenir de quoi tourner. Un dossier de donnees incomplet ne se voit qu'au
# lancement, c'est-a-dire chez le joueur.
foreach ($required in $Required) {
    if (-not (Test-Path (Join-Path $BuildDir $required))) { Fail "Element manquant dans le build : $required" }
}

# Le tampon produit PAR le build : dernier point ou l'on peut constater qu'on s'apprete a publier
# autre chose que ce qu'on croit.
# ⚠ La DATE ne prouve rien : le build Unity est incremental, un fichier identique n'est pas
# reecrit. ⚠ Les metadonnees Windows d'un .exe Unity decrivent le MOTEUR (« 6000.x »), pas le jeu.
# Seule la version embarquee, posee juste avant le build, tranche.
$stamp = Get-Content (Join-Path $BuildDir "build_stamp.json") -Raw | ConvertFrom-Json
if ($stamp.version -ne $Version) {
    Fail "Le build porte la version '$($stamp.version)' alors qu'on publie '$Version' - build perime."
}
Write-Host "Build verifie : v$($stamp.version)-$($stamp.sha) (construit le $($stamp.date))." -ForegroundColor DarkGray

# Le suffixe « + » dit que l'arbre de travail portait des modifications : le build ne correspond
# alors A AUCUN COMMIT, et le tampon affiche en jeu ne permettra pas de rejouer un rapport de bug.
if ($stamp.sha -like "*+") {
    Write-Host "AVERTISSEMENT : build issu d'un arbre modifie ($($stamp.sha)) - il ne correspond a aucun commit." -ForegroundColor Yellow
} elseif ($stamp.sha -eq "dev") {
    Write-Host "AVERTISSEMENT : le build n'a pas pu lire git - le tampon dira 'dev' aux joueurs." -ForegroundColor Yellow
}

# --- 4. Dossier de distribution propre ---------------------------------------------
# Butler diffe fichier par fichier : on pousse un DOSSIER, sans les artefacts que le build depose
# a cote (symboles Burst, qu'Unity nomme elle-meme « DoNotShip »).
if (Test-Path $Staging) { Remove-Item $Staging -Recurse -Force }
New-Item -ItemType Directory -Path $Staging -Force | Out-Null

Copy-Item (Join-Path $BuildDir "*") -Destination $Staging -Recurse -Force -Exclude "*BurstDebugInformation*"
Get-ChildItem $Staging -Directory -Filter "*BurstDebugInformation*" |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$poids = [math]::Round((Get-ChildItem $Staging -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "Staging pret : $Staging ($poids Mo)" -ForegroundColor Cyan

# --- 5. Push Butler ----------------------------------------------------------------
if ($DryRun) {
    Write-Host "`nA BLANC : tout est pret, rien n'a ete publie." -ForegroundColor Green
    Write-Host "  build   : $BuildDir" -ForegroundColor DarkGray
    Write-Host "  staging : $Staging" -ForegroundColor DarkGray
    Write-Host "  tampon  : v$($stamp.version)-$($stamp.sha)" -ForegroundColor DarkGray
    Write-Host "Relance sans -DryRun pour pousser sur $Itch`:$Channel." -ForegroundColor Green
    exit 0
}

Write-Host "Push vers itch.io..." -ForegroundColor Yellow
& $Butler push $Staging "$Itch`:$Channel" --userversion $Version
if ($LASTEXITCODE -ne 0) {
    Fail "butler push echoue (code $LASTEXITCODE). Si 'not authorized', lance une fois : `"$Butler`" login"
}

# --- 6. Manifeste de version -------------------------------------------------------
# Les joueurs qui ont TELECHARGE le jeu n'ont pas l'auto-update de l'app itch : un bandeau en jeu
# peut lire ce fichier sur raw.githubusercontent et leur annoncer la nouvelle version.
#
# ⚠ Le manifeste decrit la version TELECHARGEABLE : il n'appartient qu'a la cible Windows. Un
# joueur web est toujours a jour (la page sert le build courant). Le pousser depuis une release web
# annoncerait a tous les joueurs Windows une mise a jour qui n'existe pas.
$toCommit = @("ProjectSettings/ProjectSettings.asset")
if ($Target -eq "web") {
    Write-Host "Manifeste inchange : une release web n'annonce rien aux joueurs Windows." -ForegroundColor DarkGray
} else {
    $parts = $Itch.Split("/")
    $manifest = [ordered]@{ version = $Version; url = "https://$($parts[0]).itch.io/$($parts[1])" }
    ($manifest | ConvertTo-Json) | Out-File -FilePath (Join-Path $ProjectRoot "version.json") -Encoding utf8
    $toCommit += "version.json"
}

# --- 7. Commit du numero de version ------------------------------------------------
Push-Location $ProjectRoot
git add $toCommit
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    # Le message doit dire ce qui est REELLEMENT commite : annoncer un manifeste qu'une release web
    # ne touche pas rendrait l'historique faux la ou on vient le consulter.
    $what = if ($Target -eq "web") { "canal web" } else { "manifeste + version du projet" }
    git commit -m "chore(release): $Version ($what)"
    # ⚠ Ne PAS tester $? apres un exe natif : git ecrit sa progression sur stderr meme quand tout va
    # bien, ce qui met $? a faux alors que le code retour vaut 0.
    if ($LASTEXITCODE -eq 0) {
        git push
        if ($LASTEXITCODE -ne 0) {
            Write-Host "AVERTISSEMENT : git push echoue - pousse le commit de version a la main." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "Rien a committer (numero de version inchange)." -ForegroundColor DarkGray
}
Pop-Location

# --- 8. Etat -----------------------------------------------------------------------
& $Butler status $Itch
Write-Host "`nPublication OK - version $Version poussee sur $Itch`:$Channel" -ForegroundColor Green

if ($Target -eq "web") {
    Write-Host "La page sert le nouveau build des qu'itch a fini de le traiter." -ForegroundColor Green
    Write-Host "⚠ Prerequis cote itch.io, a faire UNE fois : « Kind of project » = HTML," -ForegroundColor Yellow
    Write-Host "  et le fichier coche « This file will be played in the browser »." -ForegroundColor Yellow
} else {
    Write-Host "Les joueurs de l'app itch.io recevront la mise a jour automatiquement." -ForegroundColor Green
}
