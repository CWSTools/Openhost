param(
    [ValidateSet("Office", "WPS", "System")]
    [string] $Target = "WPS",

    [ValidateSet("powerpoint", "word", "excel", "pdf", "ppt", "pptx", "doc", "docx", "xls", "xlsx")]
    [string[]] $Types = @("powerpoint")
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$candidatePaths = @(
    (Join-Path $repoRoot "bin\Release\net10.0\win-x64\publish\OpenHost.exe"),
    (Join-Path $repoRoot "bin\Debug\net10.0\OpenHost.exe"),
    (Join-Path $repoRoot "dist\publish\OpenHost-win-x64\OpenHost.exe"),
    (Join-Path $PSScriptRoot "..\OpenHost.exe")
)

$openHostPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $openHostPath) {
    throw "OpenHost.exe was not found. Build or unpack OpenHost first."
}

foreach ($type in $Types) {
    & $openHostPath "openhost://set-open-method?type=$type&target=$Target"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set $type to $Target."
    }

    Write-Host "$type -> $Target"
}
