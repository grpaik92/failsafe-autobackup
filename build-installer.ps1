# Build Failsafe AutoBackup Installer
# This script builds the complete MSI installer with self-contained executables

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".",
    [switch]$SkipBuild = $false,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Failsafe AutoBackup Installer Build Script ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Get repository root
$RepoRoot = $PSScriptRoot
Set-Location $RepoRoot

# Step 1: Clean previous builds
Write-Host "[1/7] Cleaning previous builds..." -ForegroundColor Green
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}
if (Test-Path "FailsafeAutoBackup.msi") {
    Remove-Item -Path "FailsafeAutoBackup.msi" -Force
}
Write-Host "  ✓ Cleaned" -ForegroundColor Gray

# Step 2: Restore dependencies
Write-Host "[2/7] Restoring dependencies..." -ForegroundColor Green
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Failed to restore dependencies" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Dependencies restored" -ForegroundColor Gray

# Step 3: Build solution
if (-not $SkipBuild) {
    Write-Host "[3/7] Building solution ($Configuration)..." -ForegroundColor Green
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ Build succeeded" -ForegroundColor Gray
} else {
    Write-Host "[3/7] Skipping build (--SkipBuild specified)" -ForegroundColor Yellow
}

# Step 4: Run tests
if (-not $SkipTests) {
    Write-Host "[4/7] Running tests..." -ForegroundColor Green
    dotnet test --configuration $Configuration --no-build --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Tests failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ Tests passed" -ForegroundColor Gray
} else {
    Write-Host "[4/7] Skipping tests (--SkipTests specified)" -ForegroundColor Yellow
}

# Step 5: Publish self-contained executables
Write-Host "[5/7] Publishing self-contained executables..." -ForegroundColor Green

# Create publish directories
New-Item -ItemType Directory -Path "publish/service" -Force | Out-Null
New-Item -ItemType Directory -Path "publish/trayapp" -Force | Out-Null

# Publish Windows Service
Write-Host "  → Publishing Windows Service..." -ForegroundColor Gray
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/service `
    --nologo `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Failed to publish Windows Service" -ForegroundColor Red
    exit 1
}

$serviceExe = Get-Item "publish/service/FailsafeAutoBackup.Service.exe"
$serviceSizeMB = [math]::Round($serviceExe.Length / 1MB, 2)
Write-Host "    ✓ Service published ($serviceSizeMB MB)" -ForegroundColor Gray

# Publish Tray Application
Write-Host "  → Publishing Tray Application..." -ForegroundColor Gray
dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/trayapp `
    --nologo `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Failed to publish Tray Application" -ForegroundColor Red
    exit 1
}

$trayExe = Get-Item "publish/trayapp/FailsafeAutoBackup.TrayApp.exe"
$traySizeMB = [math]::Round($trayExe.Length / 1MB, 2)
Write-Host "    ✓ Tray App published ($traySizeMB MB)" -ForegroundColor Gray

# Step 6: Check WiX toolset
Write-Host "[6/7] Checking WiX Toolset..." -ForegroundColor Green
$wixInstalled = $false
try {
    $wixVersion = wix --version 2>&1 | Out-String
    if ($wixVersion -match "wix version") {
        $wixInstalled = $true
        Write-Host "  ✓ WiX Toolset found: $($wixVersion.Trim())" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ✗ WiX Toolset not found" -ForegroundColor Red
}

if (-not $wixInstalled) {
    Write-Host "  → Installing WiX Toolset v4..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 4.0.5
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Failed to install WiX Toolset" -ForegroundColor Red
        Write-Host "    Please install manually: dotnet tool install --global wix --version 4.0.5" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "  ✓ WiX Toolset installed" -ForegroundColor Gray
}

# Step 7: Build MSI installer
Write-Host "[7/7] Building MSI installer..." -ForegroundColor Green

Set-Location "installer/wix"
wix build Product.wxs -ext WixToolset.UI.wixext -arch x64 -out "$RepoRoot\FailsafeAutoBackup.msi"

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Failed to build MSI installer" -ForegroundColor Red
    Set-Location $RepoRoot
    exit 1
}

Set-Location $RepoRoot

# Verify MSI was created
if (Test-Path "FailsafeAutoBackup.msi") {
    $msiFile = Get-Item "FailsafeAutoBackup.msi"
    $msiSizeMB = [math]::Round($msiFile.Length / 1MB, 2)
    Write-Host "  ✓ MSI installer created ($msiSizeMB MB)" -ForegroundColor Gray
    
    # Copy to output path if specified
    if ($OutputPath -ne ".") {
        Copy-Item "FailsafeAutoBackup.msi" -Destination $OutputPath -Force
        Write-Host "  ✓ Installer copied to: $OutputPath" -ForegroundColor Gray
    }
} else {
    Write-Host "  ✗ MSI installer not found" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host ""
Write-Host "=== Build Summary ===" -ForegroundColor Cyan
Write-Host "Windows Service: $serviceSizeMB MB (publish/service/FailsafeAutoBackup.Service.exe)" -ForegroundColor White
Write-Host "Tray Application: $traySizeMB MB (publish/trayapp/FailsafeAutoBackup.TrayApp.exe)" -ForegroundColor White
Write-Host "MSI Installer: $msiSizeMB MB (FailsafeAutoBackup.msi)" -ForegroundColor White
Write-Host ""
Write-Host "✓ Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To install: .\FailsafeAutoBackup.msi" -ForegroundColor Yellow
Write-Host "Silent install: msiexec /i FailsafeAutoBackup.msi /qn /l*v install.log" -ForegroundColor Yellow
Write-Host ""
