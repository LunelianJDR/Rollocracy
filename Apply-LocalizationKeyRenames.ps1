param(
    [string]$Root = ".",
    [switch]$WhatIf
)

$mappingPath = Join-Path $PSScriptRoot "localization_key_mapping.json"
if (-not (Test-Path $mappingPath)) {
    throw "Mapping file not found: $mappingPath"
}

$mapping = Get-Content $mappingPath -Raw | ConvertFrom-Json
$pairs = @()
$mapping.PSObject.Properties | ForEach-Object {
    $pairs += [PSCustomObject]@{
        Old = $_.Name
        New = [string]$_.Value
    }
}

# Longest keys first to avoid partial overlap issues.
$pairs = $pairs | Sort-Object { $_.Old.Length } -Descending

$includeExtensions = @(
    ".cs", ".razor", ".cshtml", ".resx", ".txt", ".json",
    ".js", ".ts", ".tsx", ".jsx", ".md"
)

$files = Get-ChildItem -Path $Root -Recurse -File | Where-Object {
    $_.FullName -notmatch "\\\.git\\" -and
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "\\obj\\" -and
    $includeExtensions -contains $_.Extension.ToLowerInvariant()
}

$changedFiles = 0
$totalReplacements = 0

foreach ($file in $files) {
    $original = Get-Content $file.FullName -Raw
    $updated = $original
    $fileReplacementCount = 0

    foreach ($pair in $pairs) {
        $pattern = '\b' + [Regex]::Escape($pair.Old) + '\b'
        $matches = [regex]::Matches($updated, $pattern).Count
        if ($matches -gt 0) {
            $updated = [regex]::Replace($updated, $pattern, $pair.New)
            $fileReplacementCount += $matches
        }
    }

    if ($fileReplacementCount -gt 0) {
        $changedFiles++
        $totalReplacements += $fileReplacementCount
        Write-Host ("UPDATED  {0}  ({1} replacement(s))" -f $file.FullName, $fileReplacementCount)

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $updated -Encoding UTF8
        }
    }
}

Write-Host ""
if ($WhatIf) {
    Write-Host ("Dry run complete. {0} file(s) would be changed, {1} replacement(s) detected." -f $changedFiles, $totalReplacements)
} else {
    Write-Host ("Done. {0} file(s) changed, {1} replacement(s) applied." -f $changedFiles, $totalReplacements)
}
