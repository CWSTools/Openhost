param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64",

    [switch] $SelfContained,

    [switch] $NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "OpenHost.csproj"
$distRoot = Join-Path $repoRoot "dist"
$publishDir = Join-Path $distRoot "publish\OpenHost-$Runtime"
$packageDir = Join-Path $distRoot "package\OpenHost-$Runtime"
$zipPath = Join-Path $distRoot "OpenHost-$Runtime.zip"

New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    $projectPath,
    "-c",
    $Configuration,
    "-r",
    $Runtime,
    "--self-contained:$($SelfContained.IsPresent.ToString().ToLowerInvariant())",
    "-o",
    $publishDir
)

if ($NoRestore) {
    $publishArgs += "--no-restore"
}

& dotnet @publishArgs

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "README.md") -Destination $packageDir -Force

$packageExamplesDir = Join-Path $packageDir "examples"
New-Item -ItemType Directory -Force -Path $packageExamplesDir | Out-Null
Copy-Item -Path (Join-Path $repoRoot "examples\switch-open-method.ps1") -Destination $packageExamplesDir -Force

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -Force

Write-Host "Package created:"
Write-Host $zipPath
