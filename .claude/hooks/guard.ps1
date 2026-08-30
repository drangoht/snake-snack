# guard.ps1 -- PreToolUse hook: refuses destructive or irreversible commands.
#
# Receives the hook JSON on stdin, answers a decision JSON on stdout.
# Complements the `deny` rules of settings.json: those match by PREFIX, whereas this script inspects
# the whole command line -- chained commands included, which is precisely how a dangerous command
# slips through unnoticed.

$ErrorActionPreference = 'Stop'

function Deny([string]$reason) {
    $payload = @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'deny'
            permissionDecisionReason = $reason
        }
    }
    $payload | ConvertTo-Json -Depth 5 -Compress
    exit 0
}

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try { $data = $raw | ConvertFrom-Json } catch { exit 0 }

$cmd = $data.tool_input.command
if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }

# The project root is derived from the hook's location: .claude\hooks\ -> two levels up. Nothing to
# substitute at install time, and the guard follows the project if it is moved.
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# The TEXT of messages (heredoc, -m "...") is not executed: exclude it from the analysis, otherwise a
# commit message that QUOTES a dangerous command blocks the commit.
$cmd = [regex]::Replace($cmd, "(?s)<<-?\s*['`"]?(\w+)['`"]?.*?(\r?\n\1|$)", ' ')
$cmd = [regex]::Replace($cmd, '(?s)(-m|--message)\s+"(?:[^"\\]|\\.)*"', ' ')
$cmd = [regex]::Replace($cmd, "(?s)(-m|--message)\s+'[^']*'", ' ')

# --- Git: history rewriting and loss of unpushed work ------------------------
if ($cmd -match '(?i)git\s+push\b' -and
    $cmd -match '(?i)(--force(?!-with-lease)|\s-f\b)') {
    Deny "git push --force is forbidden. Use --force-with-lease, or push normally."
}
if ($cmd -match '(?i)git\s+reset\s+--hard') {
    Deny "git reset --hard is forbidden: it destroys uncommitted work. Use git stash or git restore <file>."
}
if ($cmd -match '(?i)git\s+clean\s+-[a-z]*[fx]') {
    Deny "git clean -f/-x is forbidden: it deletes untracked files with no recycle bin."
}
if ($cmd -match '(?i)git\s+branch\s+-D\b') {
    Deny "git branch -D is forbidden: forced branch deletion. Use -d (merged only)."
}

# --- Recursive deletions outside the project ---------------------------------
$isRecursiveDelete =
    ($cmd -match '(?i)Remove-Item' -and $cmd -match '(?i)-Recurse') -or
    ($cmd -match '(?i)\brm\s+-[a-z]*r') -or
    ($cmd -match '(?i)\brmdir\s+/s') -or
    ($cmd -match '(?i)\brd\s+/s')

if ($isRecursiveDelete) {
    # Paths arrive in backslash (PowerShell), slash (Bash) or MSYS form (/c/Users/...). We normalise
    # everything to backslashes before testing.
    $norm = $cmd -replace '/', '\'
    $norm = [regex]::Replace($norm, '(?<![A-Za-z0-9])\\([A-Za-z])\\', '$1:\')

    if ($norm -match '(?i)(\s|["''])[A-Za-z]:\\+(["'']|\s|$)') {
        Deny "Recursive deletion at the root of a drive: refused."
    }
    if ($norm -match '(?i)(\$HOME|%USERPROFILE%|~\\|[A-Za-z]:\\Users\\)') {
        Deny "Recursive deletion targeting the user folder: refused."
    }
    if ($norm -match '(?i)[A-Za-z]:\\(Windows|Program Files|ProgramData)') {
        Deny "Recursive deletion targeting a system folder: refused."
    }
    if ($norm -match '\.\.\\') {
        Deny "Recursive deletion with a directory climb (..): refused. Use an explicit path."
    }
    # Any absolute path that leaves the project.
    foreach ($m in [regex]::Matches($norm, '(?i)[A-Za-z]:\\[^\s"'']*')) {
        if (-not $m.Value.StartsWith($projectRoot, [StringComparison]::OrdinalIgnoreCase)) {
            Deny "Recursive deletion outside the project ($($m.Value)): refused."
        }
    }
}

# --- Media destruction / secure erasure --------------------------------------
if ($cmd -match '(?i)\b(Format-Volume|Clear-Disk|diskpart|cipher\s+/w|Initialize-Disk)\b') {
    Deny "Disk media destruction command: refused."
}

exit 0
