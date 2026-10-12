#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the KnowledgeTrackerDesktop MSIX package.

.DESCRIPTION
    Publishes the WinForms app self-contained for win-x64, injects the
    Syncfusion license key embedded at build time, and packages/signs the
    result with the Windows Application Packaging Project.

    Producing an MSIX requires MSBuild from Visual Studio with the
    "Windows application packaging project" workload (the plain dotnet CLI
    cannot build .wapproj). The script locates MSBuild via vswhere.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER Platform
    Target platform (default: x64).

.PARAMETER CreateSelfSignedCertificate
    Creates a self-signed code-signing certificate for local testing and
    exports it to scripts\KnowledgeTrackerDev.pfx (not committed).

.PARAMETER PackageCertificateKeyFile
    Path to a .pfx file used to sign the package. Defaults to the generated
    scripts\KnowledgeTrackerDev.pfx when it exists.

.PARAMETER PackageCertificatePassword
    Password for the .pfx (default: the temporary password used when the
    certificate is auto-generated).

.EXAMPLE
    $env:SYNCFUSION_LICENSE_KEY = "<key>"
    .\scripts\build-msix.ps1 -CreateSelfSignedCertificate
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$CreateSelfSignedCertificate,
    [string]$CertificateSubject = "CN=KnowledgeTrackerDev",
    [string]$PackageCertificateKeyFile,
    [string]$PackageCertificatePassword = "KnowledgeTrackerDev"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$wapproj = Join-Path $repoRoot "KnowledgeTrackerDesktop.Package\KnowledgeTrackerDesktop.Package.wapproj"
$defaultPfx = Join-Path $PSScriptRoot "KnowledgeTrackerDev.pfx"

if (-not (Test-Path $wapproj)) {
    throw "Packaging project not found at $wapproj."
}

if ([string]::IsNullOrWhiteSpace($env:SYNCFUSION_LICENSE_KEY)) {
    throw "SYNCFUSION_LICENSE_KEY is not set. Set it before packaging so the license key is embedded into the build."
}

if ($CreateSelfSignedCertificate -or (-not $PackageCertificateKeyFile -and -not (Test-Path $defaultPfx))) {
    Write-Host "Creating self-signed code-signing certificate '$CertificateSubject'..."
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $CertificateSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName "KnowledgeTracker MSIX (dev)" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

    $securePassword = ConvertTo-SecureString -String $PackageCertificatePassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $defaultPfx -Password $securePassword | Out-Null

    $cerPath = [System.IO.Path]::ChangeExtension($defaultPfx, ".cer")
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

    Write-Host "Exported $defaultPfx"
    Write-Host "To install the package locally, trust the certificate (run as administrator):"
    Write-Host "    Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
}

if (-not $PackageCertificateKeyFile) {
    $PackageCertificateKeyFile = $defaultPfx
}

if (-not (Test-Path $PackageCertificateKeyFile)) {
    throw "Signing certificate not found at $PackageCertificateKeyFile. Run with -CreateSelfSignedCertificate or pass -PackageCertificateKeyFile."
}

# Locate MSBuild from Visual Studio (required for .wapproj / DesktopBridge).
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Install Visual Studio with the 'Windows application packaging project' workload."
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
    Select-Object -First 1
if (-not $msbuild) {
    throw "MSBuild.exe not found via vswhere. Install Visual Studio with the 'Windows application packaging project' workload."
}

Write-Host "Using MSBuild: $msbuild"
Write-Host "Packaging $Configuration|$Platform with license key embedded..."

$arguments = @(
    $wapproj,
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    "/p:AppxBundle=Never",
    "/p:AppxPackageSigningEnabled=true",
    "/p:PackageCertificateKeyFile=$PackageCertificateKeyFile",
    "/p:PackageCertificatePassword=$PackageCertificatePassword"
)

& $msbuild $arguments
if ($LASTEXITCODE -ne 0) {
    throw "MSIX build failed with exit code $LASTEXITCODE."
}

Write-Host "Done. The package is under $repoRoot\AppPackages\."
