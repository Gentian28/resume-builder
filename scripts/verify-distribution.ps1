param([string]$Repository = 'Gentian28/resume-builder')
$ErrorActionPreference = 'Stop'
$release = gh release view --repo $Repository --json assets,tagName,url | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Could not read public release metadata.' }
$required = @('ResumeBuilder-win-Setup.exe', 'ResumeBuilder-win-Portable.zip', 'ResumeBuilder.AppImage', 'ResumeBuilder-osx-Setup.pkg', 'ResumeBuilder-osx-x64-Setup.pkg')
foreach ($name in $required) {
    $asset = @($release.assets | Where-Object name -eq $name)
    if ($asset.Count -ne 1 -or $asset[0].size -le 0) { throw "Missing or empty distribution asset: $name" }
    if (-not $asset[0].url.StartsWith('https://github.com/' + $Repository + '/releases/download/')) { throw "Unexpected asset source: $name" }
    $response = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $asset[0].url -TimeoutSec 30
    if ($response.StatusCode -ne 200) { throw "Download did not return HTTP 200: $name" }
    Write-Output "Verified $name"
}
if (@($release.assets | Where-Object name -like 'SHA256SUMS-*').Count -lt 4) { throw 'Per-platform checksum files are missing.' }
Write-Output "Public distribution verified for $($release.tagName): $($release.url)"
