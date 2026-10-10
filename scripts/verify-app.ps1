param(
    [string]$Executable = 'src/ComicBookReader.App/bin/Release/net10.0-windows/ComicBookReader.App.exe',
    [string]$EvidenceDirectory = 'artifacts/runtime-validation',
    [switch]$Standalone,
    [string]$BundledLicensesFile = ''
)
$ErrorActionPreference = 'Stop'
$privateRuntime = Join-Path $env:LOCALAPPDATA 'ComicBookReader-dev/dotnet-10.0.401'
if (Test-Path -LiteralPath $privateRuntime) { $env:DOTNET_ROOT = $privateRuntime }
$projectRoot = Split-Path -Parent $PSScriptRoot
$executablePath = [IO.Path]::GetFullPath((Join-Path $projectRoot $Executable))
$evidenceRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $EvidenceDirectory))
[IO.Directory]::CreateDirectory($evidenceRoot) | Out-Null
$runtimeConfigPath = [IO.Path]::ChangeExtension($executablePath, 'runtimeconfig.json')
$runtimeConfig = if (Test-Path -LiteralPath $runtimeConfigPath) { Get-Content -Raw -LiteralPath $runtimeConfigPath | ConvertFrom-Json } else { $null }
if (-not $Standalone -and $null -eq $runtimeConfig) { throw 'Missing runtimeconfig; use -Standalone only for the single-file distribution.' }
if ($Standalone -or $runtimeConfig.runtimeOptions.includedFrameworks) {
    # Check the paths of loaded native modules inside the smoke test as well.
    $env:DOTNET_ROOT = Join-Path $evidenceRoot 'no-installed-runtime'
    $env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
    $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = Join-Path $evidenceRoot 'native-extraction'
    $env:COMICBOOKREADER_RUNTIME_DIRECTORY = if ($Standalone) { $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR } else { Split-Path -Parent $executablePath }
} else {
    Remove-Item Env:COMICBOOKREADER_RUNTIME_DIRECTORY -ErrorAction SilentlyContinue
}
if ($BundledLicensesFile) {
    $env:COMICBOOKREADER_LICENSES_SHA256 = (Get-FileHash -LiteralPath $BundledLicensesFile -Algorithm SHA256).Hash
} else { Remove-Item Env:COMICBOOKREADER_LICENSES_SHA256 -ErrorAction SilentlyContinue }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression
$comicPath = Join-Path $evidenceRoot 'synthetic.cbz'
if (-not (Test-Path -LiteralPath $comicPath)) {
    $archiveStream = [IO.File]::Create($comicPath)
    $archive = [IO.Compression.ZipArchive]::new($archiveStream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $sizes = @(@(1000, 12000), @(2000, 1500), @(4000, 3000))
        for ($pageIndex = 0; $pageIndex -lt $sizes.Count; $pageIndex++) {
            $bitmap = [Drawing.Bitmap]::new($sizes[$pageIndex][0], $sizes[$pageIndex][1])
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            $font = [Drawing.Font]::new('Segoe UI', 32)
            try {
                $graphics.Clear([Drawing.Color]::WhiteSmoke)
                for ($rowIndex = 0; $rowIndex -lt $bitmap.Height; $rowIndex += 200) {
                    $graphics.DrawString("ComicBookReader - pagina $($pageIndex + 1) - linha $rowIndex", $font,
                        [Drawing.Brushes]::Black, 40, $rowIndex + 40)
                    $graphics.DrawLine([Drawing.Pens]::Gray, 40, $rowIndex + 150, $bitmap.Width - 40, $rowIndex + 150)
                }
                $entry = $archive.CreateEntry("$($pageIndex + 1).png")
                $entryStream = $entry.Open()
                try { $bitmap.Save($entryStream, [Drawing.Imaging.ImageFormat]::Png) }
                finally { $entryStream.Dispose() }
            } finally { $font.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
        }
    } finally { $archive.Dispose(); $archiveStream.Dispose() }
}
$cases = @(
    @{ Name = 'cbz'; Path = $comicPath },
    @{ Name = 'rar4'; Path = (Join-Path $projectRoot 'tests/fixtures/rar4.cbr') },
    @{ Name = 'rar5'; Path = (Join-Path $projectRoot 'tests/fixtures/rar5.cbr') },
    @{ Name = 'solid'; Path = (Join-Path $projectRoot 'tests/fixtures/solid.cbr') }
)
foreach ($case in $cases) {
    $outputPath = Join-Path $evidenceRoot $case.Name
    $beforeHash = (Get-FileHash -LiteralPath $case.Path -Algorithm SHA256).Hash
    $runs = if ($case.Name -eq 'cbz') { 2 } else { 1 }
    for ($runIndex = 0; $runIndex -lt $runs; $runIndex++) {
        $arguments = '--smoke-test "{0}" "{1}"' -f $case.Path, $outputPath
        $process = Start-Process -FilePath $executablePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(60000)) {
            $process.Kill()
            throw "Smoke test timed out: $($case.Name)"
        }
        $process.Refresh()
        if ($process.ExitCode -ne 0) {
            $failurePath = Join-Path $outputPath 'failure.txt'
            if (Test-Path -LiteralPath $failurePath) { Get-Content -LiteralPath $failurePath }
            throw "Smoke test failed: $($case.Name), code $($process.ExitCode)"
        }
    }
    if ((Get-FileHash -LiteralPath $case.Path -Algorithm SHA256).Hash -ne $beforeHash) {
        throw "Smoke test modified archive: $($case.Name)"
    }
    Write-Output "PASS $($case.Name): runtime, restart when applicable, source SHA256 unchanged"
}
