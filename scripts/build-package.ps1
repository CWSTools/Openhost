param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64",

    [switch] $SelfContained,

    [switch] $NoRestore,

    [switch] $SkipSettingsUi
)

$ErrorActionPreference = "Stop"

function Find-VcVars64 {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $installPath = & $vswhere `
            -latest `
            -products * `
            -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
            -property installationPath | Select-Object -First 1
        if ($installPath) {
            $vcvars = Join-Path $installPath "VC\Auxiliary\Build\vcvars64.bat"
            if (Test-Path $vcvars) {
                return $vcvars
            }
        }
    }

    $fallbacks = @(
        "F:\VS\VC\Auxiliary\Build\vcvars64.bat",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
    )

    foreach ($fallback in $fallbacks) {
        if (Test-Path $fallback) {
            return $fallback
        }
    }

    return $null
}

function Build-SettingsUi {
    param(
        [string] $VcVars64,
        [string] $Configuration,
        [string] $RepoRoot
    )

    $sourcePath = Join-Path $RepoRoot "ui\win32\OpenHostSettings.cpp"
    $outputDir = Join-Path $RepoRoot "bin\$Configuration\win32-x64"
    $objectDir = Join-Path $RepoRoot "obj\$Configuration\win32-x64\OpenHostSettings"
    $outputPath = Join-Path $outputDir "OpenHostSettings.exe"
    $vsRoot = Resolve-Path (Join-Path (Split-Path $VcVars64 -Parent) "..\.." )
    $msvcToolsRoot = Join-Path $vsRoot "Tools\MSVC"
    $mfcHeader = Get-ChildItem -LiteralPath $msvcToolsRoot -Directory |
        ForEach-Object { Join-Path $_.FullName "atlmfc\include\afxwin.h" } |
        Where-Object { Test-Path $_ } |
        Sort-Object -Descending |
        Select-Object -First 1
    if (-not $mfcHeader) {
        throw "MFC was not found. Install the Individual component named 'C++ MFC for latest v143 build tools (x86 & x64)' or the matching MFC component for your installed toolset."
    }

    $mfcRoot = Resolve-Path (Join-Path (Split-Path $mfcHeader -Parent) "..")
    $mfcIncludeDir = Join-Path $mfcRoot "include"
    $mfcLibDir = Join-Path $mfcRoot "lib\x64"
    if (-not (Test-Path $mfcLibDir)) {
        throw "MFC x64 libraries were not found under $mfcLibDir."
    }

    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    New-Item -ItemType Directory -Force -Path $objectDir | Out-Null

    $compileFlags = @(
        "/nologo",
        "/W4",
        "/EHsc",
        "/std:c++17",
        "/utf-8",
        "/DUNICODE",
        "/D_UNICODE",
        "/D_AFXDLL",
        "/DWIN32",
        "/D_WINDOWS",
        "/D_WIN32_WINNT=0x0601",
        "/I`"$mfcIncludeDir`""
    )

    if ($Configuration -eq "Release") {
        $compileFlags += @("/O2", "/DNDEBUG", "/MD")
        $mfcLib = "mfc140u.lib"
    } else {
        $compileFlags += @("/Zi", "/Od", "/MDd")
        $mfcLib = "mfc140ud.lib"
    }

    $objectPath = Join-Path $objectDir "OpenHostSettings.obj"
    $pdbPath = Join-Path $objectDir "OpenHostSettings.pdb"
    $cmdPath = Join-Path $objectDir "build-settings-ui.cmd"
    $clCommand = @(
        "cl.exe",
        ($compileFlags -join " "),
        "`"$sourcePath`"",
        "/Fo`"$objectPath`"",
        "/Fd`"$pdbPath`"",
        "/Fe`"$outputPath`"",
        "/link",
        "/SUBSYSTEM:WINDOWS",
        "/LIBPATH:`"$mfcLibDir`"",
        $mfcLib,
        "user32.lib",
        "gdi32.lib",
        "comctl32.lib",
        "shell32.lib"
    ) -join " "

    $cmdContent = @(
        "@echo off",
        "call `"$VcVars64`" >nul",
        "if errorlevel 1 exit /b %errorlevel%",
        $clCommand
    ) -join "`r`n"
    Set-Content -LiteralPath $cmdPath -Value $cmdContent -Encoding ASCII

    & cmd.exe /d /c "`"$cmdPath`""
    if ($LASTEXITCODE -ne 0) {
        throw "OpenHostSettings.exe build failed."
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "OpenHost.csproj"
$distRoot = Join-Path $repoRoot "dist"
$publishDir = Join-Path $distRoot "publish\OpenHost-$Runtime"
$packageDir = Join-Path $distRoot "package\OpenHost-$Runtime"
$zipPath = Join-Path $distRoot "OpenHost-$Runtime.zip"
$settingsOutputPath = Join-Path $repoRoot "bin\$Configuration\win32-x64\OpenHostSettings.exe"

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

if (-not $SkipSettingsUi) {
    $vcvars = Find-VcVars64
    if ($vcvars) {
        Build-SettingsUi -VcVars64 $vcvars -Configuration $Configuration -RepoRoot $repoRoot
    } else {
        Write-Warning "Visual Studio C++ Build Tools were not found. OpenHostSettings.exe was skipped. Install Desktop development with C++ to build the Win32 UI."
    }
}

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "README.md") -Destination $packageDir -Force

if (Test-Path $settingsOutputPath) {
    Copy-Item -Path $settingsOutputPath -Destination $packageDir -Force
}

$packageExamplesDir = Join-Path $packageDir "examples"
New-Item -ItemType Directory -Force -Path $packageExamplesDir | Out-Null
Copy-Item -Path (Join-Path $repoRoot "examples\switch-open-method.ps1") -Destination $packageExamplesDir -Force

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -Force

Write-Host "Package created:"
Write-Host $zipPath
