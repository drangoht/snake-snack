<#
.SYNOPSIS
    Resolves the project's external tools (Unity, Python) and remembers the paths chosen.

.DESCRIPTION
    To dot-source:  . "$PSScriptRoot\environment.ps1"

    No installation path is written down in this repository, and that is deliberate: Unity installs
    wherever the user wants (Program Files, or another drive through the Hub's "secondary install
    path"). A guessed path always produces the same symptom for the next person:
    "Unity.exe: The term 'Unity.exe' is not recognized as the name of a cmdlet".

    Resolution order, from the most explicit to the most guessed:
      1. the -UnityPath parameter;
      2. the UNITY_PATH environment variable;
      3. tools/local.settings.json -- written on the first successful resolution, NOT versioned;
      4. the Unity Hub installations, preferring the version
         ProjectSettings/ProjectVersion.txt asks for;
      5. failing that, an error that says exactly how to supply the path.

    ⚠ No accents in the strings printed: these scripts are saved as UTF-8 without BOM, and Windows
    PowerShell 5.1 then reads the file as ANSI.
#>

$script:ProjectRoot   = Split-Path -Parent $PSScriptRoot
$script:SettingsFile  = Join-Path $PSScriptRoot "local.settings.json"

# --- Local settings (not versioned) --------------------------------------------------

function Get-LocalSettings {
    if (-not (Test-Path -LiteralPath $script:SettingsFile)) { return @{} }
    try {
        $json = Get-Content -LiteralPath $script:SettingsFile -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Write-Host "WARNING: local.settings.json unreadable, it will be rewritten." -ForegroundColor Yellow
        return @{}
    }
    $table = @{}
    if ($json) { foreach ($p in $json.PSObject.Properties) { $table[$p.Name] = $p.Value } }
    return $table
}

function Set-LocalSetting {
    param([Parameter(Mandatory = $true)][string]$Name,
          [Parameter(Mandatory = $true)][string]$Value)
    $table = Get-LocalSettings
    $table[$Name] = $Value
    # ⚠ UTF-8 WITHOUT BOM: Out-File -Encoding utf8 writes one, and Python's json.load() refuses it.
    $text = ($table | ConvertTo-Json)
    [IO.File]::WriteAllText($script:SettingsFile, $text, [Text.UTF8Encoding]::new($false))
}

# --- Unity ---------------------------------------------------------------------------

function Get-ProjectVersion {
    # ProjectVersion.txt is authoritative: it is what Unity Hub reads to decide which editor to open.
    $file = Join-Path $script:ProjectRoot "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $file)) { return "" }
    $m = Select-String -Path $file -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if ($m) { return $m.Matches[0].Groups[1].Value }
    return ""
}

function Expand-UnityPath {
    # Accepts the exe, the version folder (...\Editor\6000.x) or the ...\Editor folder: those are the
    # three forms people copy-paste from the Hub or from Explorer.
    param([string]$Raw)
    if (-not $Raw) { return "" }
    $Raw = $Raw.Trim().Trim('"')
    if (Test-Path -LiteralPath $Raw -PathType Leaf) { return (Resolve-Path -LiteralPath $Raw).Path }
    if (Test-Path -LiteralPath $Raw -PathType Container) {
        foreach ($suffix in @("Unity.exe", "Editor\Unity.exe")) {
            $attempt = Join-Path $Raw $suffix
            if (Test-Path -LiteralPath $attempt) { return (Resolve-Path -LiteralPath $attempt).Path }
        }
    }
    return ""
}

function Get-VersionKey {
    # Sort key: "6000.5.6f1" and "6000.10.1f1" compare wrongly lexicographically.
    param([string]$Version)
    $m = [regex]::Match($Version, '^(\d+)\.(\d+)\.(\d+)')
    if (-not $m.Success) { return "000000000000" }
    return "{0:D5}{1:D3}{2:D4}" -f [int]$m.Groups[1].Value, [int]$m.Groups[2].Value, [int]$m.Groups[3].Value
}

function Get-UnityRoots {
    $roots = @()
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}, (Join-Path $env:LOCALAPPDATA "Programs"))) {
        if ($base) { $roots += (Join-Path $base "Unity\Hub\Editor") }
    }
    # The Hub can install editors elsewhere (typically another drive) and records that folder here.
    # The file exists even when the option was never used: it then contains "".
    $secondary = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path -LiteralPath $secondary) {
        $path = (Get-Content -LiteralPath $secondary -Raw).Trim().Trim('"')
        if ($path) { $roots += $path.Replace('\\', '\') }
    }
    return @($roots | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique)
}

function Find-UnityInstallations {
    $found = @()
    foreach ($root in Get-UnityRoots) {
        foreach ($folder in (Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue)) {
            $exe = Join-Path $folder.FullName "Editor\Unity.exe"
            if (Test-Path -LiteralPath $exe) {
                $found += [pscustomobject]@{ Version = $folder.Name; Path = $exe }
            }
        }
    }
    return @($found | Sort-Object { Get-VersionKey $_.Version } -Descending)
}

function Resolve-UnityPath {
    <#
    .SYNOPSIS
        Returns the full path of Unity.exe, or an empty string if nothing is found.
    .PARAMETER Version
        Wanted version. Defaults to the one in ProjectVersion.txt.
    .PARAMETER Remember
        Writes the chosen path into tools/local.settings.json: never to be supplied again.
    #>
    param([string]$UnityPath = "",
          [string]$Version = "",
          [switch]$Remember,
          [switch]$Quiet)

    if (-not $Version) { $Version = Get-ProjectVersion }

    $sources = @()
    if ($UnityPath)      { $sources += @{ Name = "-UnityPath"; Value = $UnityPath } }
    if ($env:UNITY_PATH) { $sources += @{ Name = "UNITY_PATH"; Value = $env:UNITY_PATH } }
    $settings = Get-LocalSettings
    if ($settings.unityPath) { $sources += @{ Name = "tools/local.settings.json"; Value = $settings.unityPath } }

    foreach ($source in $sources) {
        $path = Expand-UnityPath $source.Value
        if ($path) {
            if (-not $Quiet) { Write-Host "Unity   : $path  ($($source.Name))" -ForegroundColor DarkGray }
            if ($Remember) { Set-LocalSetting -Name "unityPath" -Value $path }
            return $path
        }
        # A supplied but wrong path deserves reporting: it has just been typed or remembered, and
        # passing over it in silence would send people hunting for the fault elsewhere.
        Write-Host "WARNING: Unity not found through $($source.Name): $($source.Value)" -ForegroundColor Yellow
    }

    $installed = Find-UnityInstallations
    if ($installed.Count -eq 0) { return "" }

    $chosen = $installed | Where-Object { $_.Version -eq $Version } | Select-Object -First 1
    if (-not $chosen) {
        $chosen = $installed[0]
        if ($Version) {
            # ⚠ Opening a project with another editor UPGRADES the project, with no way back and
            # without asking anything in batchmode.
            Write-Host "WARNING: the project asks for Unity $Version, which is missing. Using $($chosen.Version)." -ForegroundColor Yellow
            Write-Host "  (install $Version from Unity Hub, or accept the project migration)" -ForegroundColor Yellow
        }
    }
    if (-not $Quiet) { Write-Host "Unity   : $($chosen.Path)  (detected)" -ForegroundColor DarkGray }
    if ($Remember) { Set-LocalSetting -Name "unityPath" -Value $chosen.Path }
    return $chosen.Path
}

function Get-UnityPathOrDie {
    # Variant that stops with instructions rather than letting the script carry on with an empty path
    # -- which would produce "the term ... is not recognized" thirty lines further down.
    param([string]$UnityPath = "", [string]$Version = "", [switch]$Remember, [switch]$Quiet)

    $path = Resolve-UnityPath -UnityPath $UnityPath -Version $Version -Remember:$Remember -Quiet:$Quiet
    if ($path) { return $path }

    $version = if ($Version) { $Version } else { Get-ProjectVersion }
    if (-not $version) { $version = "6000.x" }
    Write-Host ""
    Write-Host "ERROR: Unity editor not found." -ForegroundColor Red
    Write-Host "No Unity.exe in the known Unity Hub locations:" -ForegroundColor Red
    foreach ($root in Get-UnityRoots) { Write-Host "  $root" -ForegroundColor DarkGray }
    Write-Host ""
    Write-Host "Three ways to supply the path (pick one):" -ForegroundColor Yellow
    Write-Host "  1. once and for all, it will be remembered:"
    Write-Host "       & `"tools/configure.ps1`" -UnityPath `"<unity-folder>\$version\Editor\Unity.exe`"" -ForegroundColor DarkGray
    Write-Host "  2. in the session environment:"
    Write-Host "       `$env:UNITY_PATH = `"<unity-folder>\$version\Editor\Unity.exe`"" -ForegroundColor DarkGray
    Write-Host "  3. by hand in tools/local.settings.json: { `"unityPath`": `"...`" }"
    Write-Host ""
    Write-Host "Unity Hub > Installs > the cog > Show in Explorer gives the exact folder." -ForegroundColor DarkGray
    exit 1
}

# --- Python --------------------------------------------------------------------------

function Resolve-PythonCommand {
    <#
    .SYNOPSIS
        Returns something to launch Python with (`py`, `python`, or a full path), or "" if absent.
    #>
    param([string]$Python = "", [switch]$Remember)

    $candidates = @()
    if ($Python)          { $candidates += $Python }
    if ($env:PYTHON)      { $candidates += $env:PYTHON }
    $settings = Get-LocalSettings
    if ($settings.python) { $candidates += $settings.python }
    $candidates += @("py", "python3", "python")

    foreach ($candidate in $candidates) {
        $command = $null
        if ((Test-Path -LiteralPath $candidate -PathType Leaf -ErrorAction SilentlyContinue)) {
            $command = (Resolve-Path -LiteralPath $candidate).Path
        } else {
            $found = Get-Command $candidate -CommandType Application -ErrorAction SilentlyContinue |
                     Select-Object -First 1
            if ($found) { $command = $found.Source }
        }
        if ($command) {
            if ($Remember) { Set-LocalSetting -Name "python" -Value $command }
            return $command
        }
    }

    # A classic per-user installation, absent from PATH when "Add python.exe to PATH" was not ticked
    # at install time.
    $glob = Join-Path $env:LOCALAPPDATA "Programs\Python\Python3*\python.exe"
    $local = Get-ChildItem -Path $glob -ErrorAction SilentlyContinue |
             Sort-Object Name -Descending | Select-Object -First 1
    if ($local) {
        if ($Remember) { Set-LocalSetting -Name "python" -Value $local.FullName }
        return $local.FullName
    }
    return ""
}
