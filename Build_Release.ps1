param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RootDir = $PSScriptRoot
$SourcesDir = Join-Path $RootDir "Sources"
$AssemblingDir = Join-Path $RootDir "Assembling"
$FilesDir = Join-Path $RootDir "Files"
$InstallerProjDir = Join-Path $SourcesDir "Installer"
$AppProjPath = Join-Path $SourcesDir "StormUnarchiver\StormUnarchiver.csproj"
$InstallerProjPath = Join-Path $InstallerProjDir "Installer.csproj"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "       STORM UNARCHIVER -- Build & Release" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Ensure output directories exist
if (-not (Test-Path $AssemblingDir)) { New-Item -ItemType Directory -Path $AssemblingDir -Force | Out-Null }
if (-not (Test-Path $FilesDir)) { New-Item -ItemType Directory -Path $FilesDir -Force | Out-Null }

# 2. Extract version from csproj
$csprojContent = [xml](Get-Content $AppProjPath)
$version = $csprojContent.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) { $version = "0.2.0" }
Write-Host "[1/4] Project Version: v$version" -ForegroundColor Green

# 3. Publish application to Assembling
Write-Host "[2/4] Publishing application to Assembling..." -ForegroundColor Yellow
dotnet publish $AppProjPath -c $Configuration -r win-x64 --self-contained false -o $AssemblingDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error compiling application!" -ForegroundColor Red
    exit 1
}
Write-Host "  -> Assembling updated: $AssemblingDir" -ForegroundColor Green

# 4. Pack Assembling into Installer payload.zip
Write-Host "[3/4] Packaging payload.zip..." -ForegroundColor Yellow
$payloadZip = Join-Path $InstallerProjDir "payload.zip"
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
Compress-Archive -Path "$AssemblingDir\*" -DestinationPath $payloadZip -CompressionLevel Optimal
$zipSizeMb = [math]::Round((Get-Item $payloadZip).Length / 1MB, 2)
Write-Host "  -> Payload size: $zipSizeMb MB" -ForegroundColor Green

# 5. Build Installer and save to Files (preserving older versions)
Write-Host "[4/4] Building setup installer into Files..." -ForegroundColor Yellow
$tempInstallerOut = Join-Path $FilesDir "temp_build"
dotnet publish $InstallerProjPath -c $Configuration -r win-x64 -p:PublishSingleFile=true --self-contained false -o $tempInstallerOut --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error compiling installer!" -ForegroundColor Red
    exit 1
}

$installerExeName = "STORM_UNARCHIVER_v" + $version + "_Setup.exe"
$finalInstallerPath = Join-Path $FilesDir $installerExeName

# Move compiled exe to Files\
Copy-Item (Join-Path $tempInstallerOut "Installer.exe") $finalInstallerPath -Force
Remove-Item $tempInstallerOut -Recurse -Force
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "           BUILD COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Sources:    $SourcesDir" -ForegroundColor Gray
Write-Host "Assembling: $AssemblingDir\StormUnarchiver.exe" -ForegroundColor Gray
Write-Host "Installer:  $finalInstallerPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "All installers in Files (old versions preserved):" -ForegroundColor Gray
Get-ChildItem -Path $FilesDir -Filter "*.exe" | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("  - " + $_.Name + " (" + $size + " MB)") -ForegroundColor Yellow
}
