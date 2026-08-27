# run-rules-tests.ps1 -- hook PostToolUse : rejoue les tests unitaires quand la logique pure de
# Assets/Scripts/Rules/ (ou les tests eux-memes) vient d'etre modifiee.
#
# Tourne en asynchrone ; sort en code 2 si les tests cassent, ce qui REVEILLE Claude avec le detail
# de l'echec. C'est tout l'interet : une regression de regle est signalee dans la minute, sans
# qu'on ait besoin d'y penser -- et sans build Unity, puisque Rules/ ne depend pas du moteur.

$ErrorActionPreference = 'Stop'

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try { $data = $raw | ConvertFrom-Json } catch { exit 0 }

$path = $data.tool_input.file_path
if (-not $path) { $path = $data.tool_response.filePath }
if ([string]::IsNullOrWhiteSpace($path)) { exit 0 }

$norm = $path -replace '/', '\'
if ($norm -notmatch '(?i)\\Assets\\Scripts\\Rules\\.*\.cs$' -and
    $norm -notmatch '(?i)\\tests\\.*\.cs$') {
    exit 0
}

# Racine deduite de l'emplacement du hook : rien a substituer a l'installation.
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$csproj = Get-ChildItem -Path (Join-Path $projectRoot 'tests') -Filter '*.Tests.csproj' -ErrorAction SilentlyContinue |
          Select-Object -First 1
if (-not $csproj) { exit 0 }

# ⚠ Pas de test de $? apres un exe natif : dotnet ecrit sur stderr meme quand tout va bien.
$output = & dotnet test $csproj.FullName --nologo --verbosity quiet 2>&1 | Out-String

if ($LASTEXITCODE -ne 0) {
    $tail = ($output -split "`n" | Select-Object -Last 25) -join "`n"
    [Console]::Error.WriteLine("Les tests unitaires echouent apres modification de $norm :`n$tail")
    exit 2
}

exit 0
