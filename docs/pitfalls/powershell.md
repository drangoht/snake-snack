# Pitfalls — PowerShell (build and release scripts)


**⚠ NEVER test `$?` after a native executable in PowerShell 5.1.** `git`, Unity and Butler write
their progress on **stderr even when all is well**, which sets `$?` to `$false` while the return code
is 0. The release script announced "git push failed" on **every successful release**. Only
`$LASTEXITCODE` is authoritative.

**⚠ `$ErrorActionPreference = 'Stop'` is a trap in a build script**, for the same reason: the
slightest progress line on stderr aborts the script.

**⚠ A release script that can only be tried by publishing is only ever tested in production.** Hence
`-DryRun`, which goes as far as the distribution folder and stops before any visible effect.
