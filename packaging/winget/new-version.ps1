<#
.SYNOPSIS
  Generate winget manifests for a released version.

.DESCRIPTION
  Reads the published SHA256SUMS file for the given tag, so the hash always matches what is
  actually downloadable rather than a local build that may differ. Copies the previous version's
  manifests forward with version, hash and date substituted, then validates.

  Run this AFTER the release is published, because it fetches the released asset's checksum.

.EXAMPLE
  .\new-version.ps1 -Version 1.0.2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$Repo = 'Gentian28/resume-builder'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$asset = 'ResumeBuilder-win-Setup.exe'

# Newest existing manifest folder is the template to carry forward.
$previous = Get-ChildItem -Path $root -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } |
    Select-Object -Last 1
if (-not $previous) { throw "No existing manifest folder under $root to use as a template." }
Write-Host "Template: $($previous.Name)"

$sumsUrl = "https://github.com/$Repo/releases/download/v$Version/SHA256SUMS-win-x64.txt"
Write-Host "Fetching $sumsUrl"
try {
    $response = Invoke-WebRequest -Uri $sumsUrl -UseBasicParsing
} catch {
    throw "Could not fetch checksums for v$Version. Is the release published? ($_)"
}

# GitHub serves release assets as application/octet-stream, and Windows PowerShell hands back
# .Content as a byte[] for that content type rather than a string. Decode when it does.
$sums = if ($response.Content -is [byte[]]) {
    [System.Text.Encoding]::UTF8.GetString($response.Content)
} else {
    [string]$response.Content
}

$line = $sums -split "`r?`n" | Where-Object { $_ -match [regex]::Escape($asset) } | Select-Object -First 1
if (-not $line) { throw "No $asset entry in SHA256SUMS-win-x64.txt for v$Version." }
$hash = ($line -split '\s+')[0].ToUpperInvariant()
if ($hash -notmatch '^[0-9A-F]{64}$') { throw "Extracted hash does not look like SHA256: $hash" }
Write-Host "SHA256: $hash"

$target = Join-Path $root $Version
if (Test-Path $target) { throw "$target already exists - delete it first if you mean to regenerate." }
New-Item -ItemType Directory -Path $target | Out-Null

$today = (Get-Date).ToString('yyyy-MM-dd')
foreach ($file in Get-ChildItem -Path $previous.FullName -Filter *.yaml) {
    $text = Get-Content $file.FullName -Raw
    $text = $text -replace 'PackageVersion: .+', "PackageVersion: $Version"
    $text = $text -replace 'InstallerSha256: [0-9A-Fa-f]{64}', "InstallerSha256: $hash"
    $text = $text -replace 'ReleaseDate: \d{4}-\d{2}-\d{2}', "ReleaseDate: $today"
    $text = $text -replace 'releases/download/v[0-9.]+/', "releases/download/v$Version/"
    $text = $text -replace 'releases/tag/v[0-9.]+', "releases/tag/v$Version"
    Set-Content -Path (Join-Path $target $file.Name) -Value $text -Encoding utf8 -NoNewline
}

Write-Host "`nWrote $target"
winget validate --manifest $target
if ($LASTEXITCODE -ne 0) { throw "winget validate failed with exit code $LASTEXITCODE." }

Write-Host @"

Next:
  1. winget install --manifest "$target"      # verify it really installs
  2. Fork/refresh github.com/microsoft/winget-pkgs
  3. Copy these files to manifests/g/Gentian28/ResumeBuilder/$Version/
  4. Open a PR titled: New version: Gentian28.ResumeBuilder version $Version
"@
