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
Write-Host "       STORM UNARCHIVER -- Build, Sign & Release" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Ensure output directories exist
if (-not (Test-Path $AssemblingDir)) { New-Item -ItemType Directory -Path $AssemblingDir -Force | Out-Null }
if (-not (Test-Path $FilesDir)) { New-Item -ItemType Directory -Path $FilesDir -Force | Out-Null }

# 2. Get / Create Code Signing Certificate
Write-Host "[1/5] Checking Code Signing certificate..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*CN=STORM TEAM*" } | Select-Object -First 1
if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=STORM TEAM, O=STORM TEAM" -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(10)
    $rootStore = [System.Security.Cryptography.X509Certificates.X509Store]::new("Root", "CurrentUser")
    $rootStore.Open("ReadWrite")
    $rootStore.Add($cert)
    $rootStore.Close()
    
    $pubStore = [System.Security.Cryptography.X509Certificates.X509Store]::new("TrustedPublisher", "CurrentUser")
    $pubStore.Open("ReadWrite")
    $pubStore.Add($cert)
    $pubStore.Close()
}
$thumb = $cert.Thumbprint
$subj = $cert.Subject
Write-Host "  -> Certificate: $subj [$thumb]" -ForegroundColor Green

$CertExportPath = Join-Path $RootDir "STORM_Certificate.cer"
Export-Certificate -Cert $cert -FilePath $CertExportPath -Force | Out-Null
Write-Host "  -> Exported to: $CertExportPath" -ForegroundColor Green

# 3. Extract version from csproj
$csprojContent = [xml](Get-Content $AppProjPath)
$version = $csprojContent.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) { $version = "1.0.0" }
Write-Host "[2/5] Project Version: v$version" -ForegroundColor Green

# 4. Publish application to Assembling (Self-Contained)
Write-Host "[3/5] Publishing StormUnarchiver to Assembling (Self-Contained)..." -ForegroundColor Yellow
Stop-Process -Name StormUnarchiver -Force -ErrorAction SilentlyContinue
dotnet publish $AppProjPath -c $Configuration -r win-x64 --self-contained true -o $AssemblingDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error compiling application!" -ForegroundColor Red
    exit 1
}

# Ensure resources.pri exists for WinUI 3 resource loader
if (Test-Path "$AssemblingDir\StormUnarchiver.pri") {
    Copy-Item "$AssemblingDir\StormUnarchiver.pri" "$AssemblingDir\resources.pri" -Force
}

# Unblock and digitally sign Assembling binaries
Get-ChildItem -Path $AssemblingDir -Recurse | Unblock-File -ErrorAction SilentlyContinue
Set-AuthenticodeSignature -FilePath "$AssemblingDir\StormUnarchiver.exe" -Certificate $cert | Out-Null
Set-AuthenticodeSignature -FilePath "$AssemblingDir\StormUnarchiver.dll" -Certificate $cert | Out-Null
Write-Host "  -> Assembling built and signed: $AssemblingDir" -ForegroundColor Green

# 5. Pack Assembling into Installer payload.zip
Write-Host "[4/5] Packaging payload.zip for installer..." -ForegroundColor Yellow
$payloadZip = Join-Path $InstallerProjDir "payload.zip"
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
Compress-Archive -Path "$AssemblingDir\*" -DestinationPath $payloadZip -CompressionLevel Optimal
$zipSizeMb = [math]::Round((Get-Item $payloadZip).Length / 1MB, 2)
Write-Host "  -> Payload size: $zipSizeMb MB" -ForegroundColor Green

# 6. Build Installer and save to Files (preserving older versions)
Write-Host "[5/5] Building installer executable into Files..." -ForegroundColor Yellow
$tempInstallerOut = Join-Path $FilesDir "temp_build"
dotnet publish $InstallerProjPath -c $Configuration -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true --self-contained true -o $tempInstallerOut --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error compiling installer!" -ForegroundColor Red
    exit 1
}

$installerExeName = "STORM_UNARCHIVER_" + $version + "_Setup.exe"
$finalInstallerPath = Join-Path $FilesDir $installerExeName

# Move compiled exe to Files\
Copy-Item (Join-Path $tempInstallerOut "Installer.exe") $finalInstallerPath -Force
Remove-Item $tempInstallerOut -Recurse -Force
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }

# Unblock and Sign Setup.exe
Unblock-File -Path $finalInstallerPath -ErrorAction SilentlyContinue
Set-AuthenticodeSignature -FilePath $finalInstallerPath -Certificate $cert | Out-Null

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "           BUILD COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "[-] Sources:    $SourcesDir" -ForegroundColor Gray
Write-Host "[-] Assembling: $AssemblingDir\StormUnarchiver.exe" -ForegroundColor Gray
Write-Host "[-] Installer:  $finalInstallerPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installers in Files:" -ForegroundColor Gray
Get-ChildItem -Path $FilesDir -Filter "*.exe" | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    $msg = "  - " + $_.Name + " (" + $size + " MB)"
    Write-Host $msg -ForegroundColor Yellow
}
