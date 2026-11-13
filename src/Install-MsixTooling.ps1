#requires -Version 5.1
<#
.SYNOPSIS
	Detects and installs missing Visual Studio components for MSIX packaging.

.DESCRIPTION
	Self-elevating script that checks for required VS components and installs only what's missing.
	Does not modify the machine if all components are already present.
#>
param(
	[string[]] $ComponentIds = @(
		'Microsoft.VisualStudio.Component.EnableMsixTooling',
		'Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK.Cs',
		'Microsoft.VisualStudio.Component.Windows11SDK.22000'
	)
)

# Self-elevate if not running as admin
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal] $identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
	Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
	Start-Process powershell.exe -Verb RunAs -ArgumentList (
		@('-NoProfile','-ExecutionPolicy','Bypass','-File',$PSCommandPath) + $ComponentIds)
	return
}

Write-Host "Checking Visual Studio installation..." -ForegroundColor Cyan

# Locate vswhere.exe
$installerDir = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
$vswhere = Join-Path $installerDir 'vswhere.exe'
if (-not (Test-Path $vswhere)) {
	throw 'vswhere.exe not found. Is Visual Studio 2017+ installed?'
}

# Find latest VS installation
$installPath = & $vswhere -latest -products * -property installationPath
if (-not $installPath) {
	throw 'No Visual Studio installation found.'
}

Write-Host "Found Visual Studio at: $installPath" -ForegroundColor Green

# Check which components are missing
Write-Host "`nChecking required components..." -ForegroundColor Cyan
$missing = @()
foreach ($id in $ComponentIds) {
	$installedPaths = @(& $vswhere -products * -requires $id -property installationPath)
	$installed = $installedPaths | Where-Object {
		[string]::Equals(
			[IO.Path]::GetFullPath($_).TrimEnd('\'),
			[IO.Path]::GetFullPath($installPath).TrimEnd('\'),
			[StringComparison]::OrdinalIgnoreCase)
	}
	if ($installed) {
		Write-Host "  [✓] $id" -ForegroundColor Green
	} else {
		Write-Host "  [✗] $id" -ForegroundColor Red
		$missing += $id
	}
}

if (-not $missing) {
	Write-Host "`n✓ All required components are already installed!" -ForegroundColor Green
	Write-Host "`nIf Visual Studio is open, please restart it to load the MSIX project." -ForegroundColor Yellow
	Read-Host "`nPress Enter to exit"
	return
}

# Install missing components
Write-Host "`n" -NoNewline
Write-Host "Missing components detected. Installing:" -ForegroundColor Yellow
$missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }

$addArgs = $missing | ForEach-Object { '--add', $_ }
$setup = Join-Path $installerDir 'setup.exe'

Write-Host "`nStarting Visual Studio installer..." -ForegroundColor Cyan
Write-Host "(This may take several minutes)" -ForegroundColor Gray

& $setup modify --installPath $installPath @addArgs --quiet --norestart --wait
if ($LASTEXITCODE -ne 0) {
	throw "Visual Studio Installer failed with exit code $LASTEXITCODE."
}

Write-Host "`n✓ Installation complete!" -ForegroundColor Green
Write-Host "`nPlease restart Visual Studio to load the MSIX project." -ForegroundColor Yellow
Read-Host "`nPress Enter to exit"
