param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $tracked = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    git -c core.quotePath=false ls-files | ForEach-Object { [void]$tracked.Add($_) }
    if ($LASTEXITCODE -ne 0 -or $tracked.Count -eq 0) { throw 'No tracked project files found.' }
    $missing = [System.Collections.Generic.List[string]]::new()
    $count = 0
    foreach ($dir in @('UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','raw','unity','LevelData','inventory')) {
        Get-ChildItem -LiteralPath $dir -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($root.Length + 1).Replace('\','/')
            if (-not $tracked.Contains($relative)) { $missing.Add($relative) }
            $count++
        }
    }
    foreach ($line in (Get-Content 'UnityProject/ProjectSettings/EditorBuildSettings.asset')) {
        if ($line -match '^\s+path: (.+)$') {
            $scene = 'UnityProject/' + $Matches[1]
            if (-not $tracked.Contains($scene) -or -not $tracked.Contains($scene + '.meta')) { $missing.Add($scene) }
        }
    }
    if ($missing.Count) { $missing | Select-Object -First 30 | Write-Output; throw "$($missing.Count) required files are not tracked." }
    Write-Output "PASS: $count core/source files tracked; all configured build scenes and their metadata tracked."
    git fsck --full --no-dangling
    if ($LASTEXITCODE -ne 0) { throw 'Git object integrity check failed.' }
} finally { Pop-Location }

