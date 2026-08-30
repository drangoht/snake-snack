# run-rules-tests.ps1 -- PostToolUse hook: replays the unit tests when the pure logic of
# Assets/Scripts/Rules/ (or the tests themselves) has just been modified.
#
# Runs asynchronously; exits with code 2 if the tests break, which WAKES Claude up with the detail of
# the failure. That is the whole point: a rule regression is reported within the minute, without
# anyone having to think about it -- and with no Unity build, since Rules/ does not depend on the
# engine.

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

# Root derived from the hook's location: nothing to substitute at install time.
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$csproj = Get-ChildItem -Path (Join-Path $projectRoot 'tests') -Filter '*.Tests.csproj' -ErrorAction SilentlyContinue |
          Select-Object -First 1
if (-not $csproj) { exit 0 }

# ⚠ No test of $? after a native exe: dotnet writes on stderr even when all is well.
$output = & dotnet test $csproj.FullName --nologo --verbosity quiet 2>&1 | Out-String

if ($LASTEXITCODE -ne 0) {
    $tail = ($output -split "`n" | Select-Object -Last 25) -join "`n"
    [Console]::Error.WriteLine("The unit tests fail after modifying $norm :`n$tail")
    exit 2
}

exit 0
