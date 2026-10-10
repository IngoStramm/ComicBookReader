param([string]$DotNet = (Join-Path $env:LOCALAPPDATA 'ComicBookReader-dev/dotnet-10.0.401/dotnet.exe'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $projectRoot 'artifacts/win-x64'
Push-Location $projectRoot
try {
    rtk proxy $DotNet publish src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Release --runtime win-x64 --self-contained true --output $destination -p:RestoreLockedMode=true -p:RestorePackagesWithLockFile=true
    if ($LASTEXITCODE -ne 0) { throw 'Locked publication failed.' }
    $packageCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
    $legalDirectory = Join-Path $destination 'licenses'
    [IO.Directory]::CreateDirectory($legalDirectory) | Out-Null
    $copies = @{
        'tests/fixtures/SHARPCOMPRESS-LICENSE.txt' = 'SharpCompress-LICENSE.txt'
        'README.md' = '../README.md'
        'THIRD-PARTY-NOTICES.md' = '../THIRD-PARTY-NOTICES.md'
    }
    foreach ($entry in $copies.GetEnumerator()) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $entry.Key) -Destination (Join-Path $legalDirectory $entry.Value)
    }
    $documentationDirectory = Join-Path $destination 'docs'
    [IO.Directory]::CreateDirectory($documentationDirectory) | Out-Null
    foreach ($documentName in @('manual-validation.md', 'acceptance.md', 'review.md')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot "docs/$documentName") -Destination (Join-Path $documentationDirectory $documentName)
    }
    $infrastructureLock = Get-Content -Raw -LiteralPath 'src/ComicBookReader.Infrastructure/packages.lock.json' | ConvertFrom-Json
    $resolved = $infrastructureLock.dependencies.'net10.0'
    $skiaVersion = $resolved.SkiaSharp.resolved
    $sharpVersion = $resolved.SharpCompress.resolved
    Copy-Item -LiteralPath (Join-Path $packageCache "skiasharp/$skiaVersion/LICENSE.txt") -Destination (Join-Path $legalDirectory 'SkiaSharp-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $packageCache "skiasharp.nativeassets.win32/$skiaVersion/THIRD-PARTY-NOTICES.txt") -Destination (Join-Path $legalDirectory 'SkiaSharp-THIRD-PARTY-NOTICES.txt')
    Copy-Item -LiteralPath (Join-Path $packageCache "sharpcompress/$sharpVersion/README.md") -Destination (Join-Path $legalDirectory 'SharpCompress-README.md')
    $runtimeConfig = Get-Content -Raw -LiteralPath (Join-Path $destination 'ComicBookReader.App.runtimeconfig.json') | ConvertFrom-Json
    foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
        $runtimePackage = switch ($framework.name) {
            'Microsoft.NETCore.App' { 'microsoft.netcore.app.runtime.win-x64' }
            'Microsoft.WindowsDesktop.App' { 'microsoft.windowsdesktop.app.runtime.win-x64' }
            default { throw "Unexpected runtime framework: $($framework.name)" }
        }
        $runtimePackagePath = Join-Path $packageCache "$runtimePackage/$($framework.version)"
        $licenseName = if ($framework.name -eq 'Microsoft.WindowsDesktop.App') { 'LICENSE' } else { 'LICENSE.TXT' }
        Copy-Item -LiteralPath (Join-Path $runtimePackagePath $licenseName) -Destination (Join-Path $legalDirectory "$runtimePackage-LICENSE.txt")
        if ($framework.name -eq 'Microsoft.NETCore.App') {
            Copy-Item -LiteralPath (Join-Path $runtimePackagePath 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $legalDirectory "$runtimePackage-THIRD-PARTY-NOTICES.txt")
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $destination 'libSkiaSharp.dll'))) { throw 'Missing native image decoder.' }
    Write-Output "Local package ready: $destination"
} finally { Pop-Location }
