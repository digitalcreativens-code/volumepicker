# Run on Windows with .NET 8 SDK installed.
# Produces a self-contained single-file EXE under dist/win-x64/

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "src/VolumeGuard/VolumeGuard.csproj"
$out = Join-Path $root "dist/win-x64"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet nije u PATH. Instaliraj .NET 8 SDK sa https://dotnet.microsoft.com/download/dotnet/8.0"
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    -o $out

Write-Host ""
Write-Host "Gotovo. EXE:" (Join-Path $out "VolumeGuard.exe")
Write-Host "Kopiraj VolumeGuard.exe na drugi PC i pokreni (Windows 10/11 x64)."
