Param(
    [string]$OutDir = ".\dist",
    [string]$Configuration = "Release",
    [string]$Project = "src\LeXtudio.Windows\LeXtudio.Windows.csproj"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$ProjectPath = if ([System.IO.Path]::IsPathRooted($Project)) { $Project } else { Join-Path $RepoRoot $Project }

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

$OutputPath = if ([System.IO.Path]::IsPathRooted($OutDir)) { $OutDir } else { Join-Path $RepoRoot $OutDir }
function Find-MSBuild {
    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($programFilesX86) {
        $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path $vswhere) {
            $installPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
            if ($installPath) {
                $candidate = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
                if (Test-Path $candidate) {
                    return (Resolve-Path $candidate).Path
                }
            }
        }
    }

    $command = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Path
    }

    throw "MSBuild was not found. Install Visual Studio/MSBuild to pack the Windows/WPF target."
}

function Resolve-ProjectPath([string]$Project) {
    if (Test-Path $Project) {
        return (Resolve-Path $Project).Path
    }

    $candidate = Join-Path $RepoRoot $Project
    if (Test-Path $candidate) {
        return (Resolve-Path $candidate).Path
    }

    throw "Project file not found: $Project"
}

function Reset-Directory([string]$Path) {
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path | Out-Null
}

Reset-Directory $OutputPath

$msbuild = Find-MSBuild
Write-Host "MSBuild: $msbuild"
Write-Host "Packing project: $ProjectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Output path: $OutputPath"

$previousOutDir = [Environment]::GetEnvironmentVariable("OutDir", "Process")
[Environment]::SetEnvironmentVariable("OutDir", $null, "Process")
try {
    & $msbuild $ProjectPath /restore /t:Pack /p:Configuration=$Configuration /p:PackageOutputPath=$OutputPath /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild pack failed with exit code $LASTEXITCODE"
    }
} finally {
    [Environment]::SetEnvironmentVariable("OutDir", $previousOutDir, "Process")
}

Write-Host "Package created in: $OutputPath"
Get-ChildItem -Path $OutputPath -File | ForEach-Object { Write-Host "  $($_.Name)" }
