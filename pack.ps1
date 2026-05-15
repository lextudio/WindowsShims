Param(
    [string]$OutDir = ".\dist",
    [string]$Configuration = "Release",
    [string]$Project = "src\LeXtudio.Windows\LeXtudio.Windows.csproj"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot $Project

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

$OutputPath = Join-Path $RepoRoot $OutDir
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

Write-Host "Packing project: $ProjectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Output path: $OutputPath"

dotnet pack $ProjectPath -c $Configuration -o $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed with exit code $LASTEXITCODE"
}

Write-Host "Package created in: $OutputPath"
Get-ChildItem -Path $OutputPath -File | ForEach-Object { Write-Host "  $($_.Name)" }
