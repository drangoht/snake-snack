<#
.SYNOPSIS
    Resout les outils externes du projet (Unity, Python) et memorise les chemins retenus.

.DESCRIPTION
    A dot-sourcer :  . "$PSScriptRoot\environnement.ps1"

    Aucun chemin d'installation n'est ecrit en dur dans ce depot, et c'est delibere : Unity
    s'installe ou l'utilisateur veut (Program Files, ou un autre disque via le « secondary install
    path » du Hub). Un chemin devine produit toujours le meme symptome chez le suivant :
    « Unity.exe : Le terme «Unity.exe» n'est pas reconnu comme nom d'applet de commande ».

    Ordre de resolution, du plus explicite au plus devine :
      1. le parametre -UnityPath ;
      2. la variable d'environnement UNITY_PATH ;
      3. tools/local.settings.json -- ecrit a la premiere resolution reussie, NON versionne ;
      4. les installations d'Unity Hub, en preferant la version que reclame
         ProjectSettings/ProjectVersion.txt ;
      5. a defaut, une erreur qui dit exactement comment donner le chemin.

    ⚠ Pas d'accents dans les chaines affichees : ces scripts sont enregistres en UTF-8 sans BOM, et
    Windows PowerShell 5.1 lit alors le fichier en ANSI.
#>

$script:RacineProjet    = Split-Path -Parent $PSScriptRoot
$script:FichierReglages = Join-Path $PSScriptRoot "local.settings.json"

# --- Reglages locaux (non versionnes) ------------------------------------------------

function Get-ReglagesLocaux {
    if (-not (Test-Path -LiteralPath $script:FichierReglages)) { return @{} }
    try {
        $json = Get-Content -LiteralPath $script:FichierReglages -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Write-Host "AVERTISSEMENT : local.settings.json illisible, il sera reecrit." -ForegroundColor Yellow
        return @{}
    }
    $table = @{}
    if ($json) { foreach ($p in $json.PSObject.Properties) { $table[$p.Name] = $p.Value } }
    return $table
}

function Set-ReglageLocal {
    param([Parameter(Mandatory = $true)][string]$Nom,
          [Parameter(Mandatory = $true)][string]$Valeur)
    $table = Get-ReglagesLocaux
    $table[$Nom] = $Valeur
    # ⚠ UTF-8 SANS BOM : Out-File -Encoding utf8 en pose un, et json.load() de Python le refuse.
    $texte = ($table | ConvertTo-Json)
    [IO.File]::WriteAllText($script:FichierReglages, $texte, [Text.UTF8Encoding]::new($false))
}

# --- Unity ---------------------------------------------------------------------------

function Get-VersionProjet {
    # ProjectVersion.txt fait foi : c'est lui qu'Unity Hub lit pour decider quel editeur ouvrir.
    $fichier = Join-Path $script:RacineProjet "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $fichier)) { return "" }
    $m = Select-String -Path $fichier -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if ($m) { return $m.Matches[0].Groups[1].Value }
    return ""
}

function Expand-CheminUnity {
    # Accepte l'exe, le dossier de version (...\Editor\6000.x) ou le dossier ...\Editor : ce sont
    # les trois formes qu'on copie-colle depuis le Hub ou l'explorateur.
    param([string]$Brut)
    if (-not $Brut) { return "" }
    $Brut = $Brut.Trim().Trim('"')
    if (Test-Path -LiteralPath $Brut -PathType Leaf) { return (Resolve-Path -LiteralPath $Brut).Path }
    if (Test-Path -LiteralPath $Brut -PathType Container) {
        foreach ($suffixe in @("Unity.exe", "Editor\Unity.exe")) {
            $essai = Join-Path $Brut $suffixe
            if (Test-Path -LiteralPath $essai) { return (Resolve-Path -LiteralPath $essai).Path }
        }
    }
    return ""
}

function Get-CleVersion {
    # Clef de tri : "6000.5.6f1" et "6000.10.1f1" se comparent faux en lexicographique.
    param([string]$Version)
    $m = [regex]::Match($Version, '^(\d+)\.(\d+)\.(\d+)')
    if (-not $m.Success) { return "000000000000" }
    return "{0:D5}{1:D3}{2:D4}" -f [int]$m.Groups[1].Value, [int]$m.Groups[2].Value, [int]$m.Groups[3].Value
}

function Get-RacinesUnity {
    $racines = @()
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}, (Join-Path $env:LOCALAPPDATA "Programs"))) {
        if ($base) { $racines += (Join-Path $base "Unity\Hub\Editor") }
    }
    # Le Hub sait installer les editeurs ailleurs (typiquement un autre disque) et note ce dossier
    # ici. Le fichier existe meme quand l'option n'a jamais servi : il contient alors "".
    $secondaire = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path -LiteralPath $secondaire) {
        $chemin = (Get-Content -LiteralPath $secondaire -Raw).Trim().Trim('"')
        if ($chemin) { $racines += $chemin.Replace('\\', '\') }
    }
    return @($racines | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique)
}

function Find-UnityInstallations {
    $trouvees = @()
    foreach ($racine in Get-RacinesUnity) {
        foreach ($dossier in (Get-ChildItem -LiteralPath $racine -Directory -ErrorAction SilentlyContinue)) {
            $exe = Join-Path $dossier.FullName "Editor\Unity.exe"
            if (Test-Path -LiteralPath $exe) {
                $trouvees += [pscustomobject]@{ Version = $dossier.Name; Chemin = $exe }
            }
        }
    }
    return @($trouvees | Sort-Object { Get-CleVersion $_.Version } -Descending)
}

function Resolve-UnityPath {
    <#
    .SYNOPSIS
        Renvoie le chemin complet d'Unity.exe, ou une chaine vide si rien n'est trouve.
    .PARAMETER Version
        Version souhaitee. Par defaut celle de ProjectVersion.txt.
    .PARAMETER Memoriser
        Ecrit le chemin retenu dans tools/local.settings.json : plus jamais a le redonner.
    #>
    param([string]$UnityPath = "",
          [string]$Version = "",
          [switch]$Memoriser,
          [switch]$Silencieux)

    if (-not $Version) { $Version = Get-VersionProjet }

    $sources = @()
    if ($UnityPath)      { $sources += @{ Nom = "-UnityPath"; Valeur = $UnityPath } }
    if ($env:UNITY_PATH) { $sources += @{ Nom = "UNITY_PATH"; Valeur = $env:UNITY_PATH } }
    $reglages = Get-ReglagesLocaux
    if ($reglages.unityPath) { $sources += @{ Nom = "tools/local.settings.json"; Valeur = $reglages.unityPath } }

    foreach ($source in $sources) {
        $chemin = Expand-CheminUnity $source.Valeur
        if ($chemin) {
            if (-not $Silencieux) { Write-Host "Unity   : $chemin  ($($source.Nom))" -ForegroundColor DarkGray }
            if ($Memoriser) { Set-ReglageLocal -Nom "unityPath" -Valeur $chemin }
            return $chemin
        }
        # Un chemin donne mais faux merite d'etre signale : il vient d'etre saisi ou memorise, et
        # le passer sous silence enverrait chercher la panne ailleurs.
        Write-Host "AVERTISSEMENT : Unity introuvable via $($source.Nom) : $($source.Valeur)" -ForegroundColor Yellow
    }

    $installees = Find-UnityInstallations
    if ($installees.Count -eq 0) { return "" }

    $choisie = $installees | Where-Object { $_.Version -eq $Version } | Select-Object -First 1
    if (-not $choisie) {
        $choisie = $installees[0]
        if ($Version) {
            # ⚠ Ouvrir un projet avec un autre editeur MET A JOUR le projet, sans retour arriere et
            # sans rien demander en batchmode.
            Write-Host "AVERTISSEMENT : le projet demande Unity $Version, absent. Utilisation de $($choisie.Version)." -ForegroundColor Yellow
            Write-Host "  (installer $Version depuis Unity Hub, ou accepter la migration du projet)" -ForegroundColor Yellow
        }
    }
    if (-not $Silencieux) { Write-Host "Unity   : $($choisie.Chemin)  (detecte)" -ForegroundColor DarkGray }
    if ($Memoriser) { Set-ReglageLocal -Nom "unityPath" -Valeur $choisie.Chemin }
    return $choisie.Chemin
}

function Get-UnityPathOuMourir {
    # Variante qui s'arrete avec un mode d'emploi plutot que de laisser le script continuer avec un
    # chemin vide -- ce qui produirait « le terme ... n'est pas reconnu » trente lignes plus loin.
    param([string]$UnityPath = "", [string]$Version = "", [switch]$Memoriser, [switch]$Silencieux)

    $chemin = Resolve-UnityPath -UnityPath $UnityPath -Version $Version -Memoriser:$Memoriser -Silencieux:$Silencieux
    if ($chemin) { return $chemin }

    $version = if ($Version) { $Version } else { Get-VersionProjet }
    if (-not $version) { $version = "6000.x" }
    Write-Host ""
    Write-Host "ERREUR : editeur Unity introuvable." -ForegroundColor Red
    Write-Host "Aucun Unity.exe dans les emplacements connus d'Unity Hub :" -ForegroundColor Red
    foreach ($racine in Get-RacinesUnity) { Write-Host "  $racine" -ForegroundColor DarkGray }
    Write-Host ""
    Write-Host "Trois facons de donner le chemin (au choix) :" -ForegroundColor Yellow
    Write-Host "  1. une fois pour toutes, il sera memorise :"
    Write-Host "       & `"tools/configurer.ps1`" -UnityPath `"<dossier-unity>\$version\Editor\Unity.exe`"" -ForegroundColor DarkGray
    Write-Host "  2. dans l'environnement de la session :"
    Write-Host "       `$env:UNITY_PATH = `"<dossier-unity>\$version\Editor\Unity.exe`"" -ForegroundColor DarkGray
    Write-Host "  3. a la main dans tools/local.settings.json : { `"unityPath`": `"...`" }"
    Write-Host ""
    Write-Host "Unity Hub > Installs > la roue dentee > Show in Explorer donne le dossier exact." -ForegroundColor DarkGray
    exit 1
}

# --- Python --------------------------------------------------------------------------

function Resolve-PythonCommand {
    <#
    .SYNOPSIS
        Renvoie de quoi lancer Python (`py`, `python`, ou un chemin complet), ou "" si absent.
    #>
    param([string]$Python = "", [switch]$Memoriser)

    $candidats = @()
    if ($Python)          { $candidats += $Python }
    if ($env:PYTHON)      { $candidats += $env:PYTHON }
    $reglages = Get-ReglagesLocaux
    if ($reglages.python) { $candidats += $reglages.python }
    $candidats += @("py", "python3", "python")

    foreach ($candidat in $candidats) {
        $commande = $null
        if ((Test-Path -LiteralPath $candidat -PathType Leaf -ErrorAction SilentlyContinue)) {
            $commande = (Resolve-Path -LiteralPath $candidat).Path
        } else {
            $trouve = Get-Command $candidat -CommandType Application -ErrorAction SilentlyContinue |
                      Select-Object -First 1
            if ($trouve) { $commande = $trouve.Source }
        }
        if ($commande) {
            if ($Memoriser) { Set-ReglageLocal -Nom "python" -Valeur $commande }
            return $commande
        }
    }

    # Installation utilisateur classique, absente du PATH quand « Add python.exe to PATH » n'a pas
    # ete coche a l'installation.
    $glob = Join-Path $env:LOCALAPPDATA "Programs\Python\Python3*\python.exe"
    $local = Get-ChildItem -Path $glob -ErrorAction SilentlyContinue |
             Sort-Object Name -Descending | Select-Object -First 1
    if ($local) {
        if ($Memoriser) { Set-ReglageLocal -Nom "python" -Valeur $local.FullName }
        return $local.FullName
    }
    return ""
}
