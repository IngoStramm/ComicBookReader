[CmdletBinding()]
param(
    [string]$Iscc = (Join-Path $env:LOCALAPPDATA 'ComicBookReader-dev/inno-6.7.3/ISCC.exe')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$distributionDirectory = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts/win-x64-distribution'))
$applicationExecutable = Join-Path $distributionDirectory 'ComicBookReader.App.exe'
$applicationIcon = Join-Path $projectRoot 'src/ComicBookReader.App/Assets/comic-reader.ico'
$installerScript = Join-Path $projectRoot 'installer/ComicBookReader.iss'
$outputDirectory = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts/distribution'))

if (-not (Test-Path -LiteralPath $distributionDirectory -PathType Container)) {
    throw "Published distribution not found: $distributionDirectory. Run scripts/publish.ps1 first."
}
if (-not (Test-Path -LiteralPath $applicationExecutable -PathType Leaf)) {
    throw "Published distribution is missing the expected executable: $applicationExecutable"
}
if (-not (Test-Path -LiteralPath $applicationIcon -PathType Leaf)) {
    throw "Application icon not found: $applicationIcon"
}
if (-not (Test-Path -LiteralPath $installerScript -PathType Leaf)) {
    throw "Inno Setup script not found: $installerScript"
}

$isccPath = [IO.Path]::GetFullPath($Iscc)
if (-not (Test-Path -LiteralPath $isccPath -PathType Leaf)) {
    throw "Inno Setup 6.7.3 compiler not found at '$isccPath'. Pass its path with -Iscc."
}
# The compiler's own preprocessor checks its version in ComicBookReader.iss.
# ISCC.exe does not provide useful FileVersion metadata.

$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($applicationExecutable).FileVersion
if ([string]::IsNullOrWhiteSpace($fileVersion)) {
    throw "Published executable has no FileVersion metadata: $applicationExecutable"
}
if ($fileVersion -notmatch '^(?<version>\d+\.\d+\.\d+)(?:\.\d+)?$') {
    throw "Published executable has an unsupported FileVersion '$fileVersion'; expected a numeric major.minor.patch version."
}
$appVersion = $Matches.version

[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$compilerArguments = @(
    '/Q'
    "/DAppVersion=`"$appVersion`""
    "/O$outputDirectory"
    $installerScript
)
rtk proxy $isccPath @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $outputDirectory "ComicBookReader-$appVersion-Setup.exe"
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Compiler reported success but the expected installer was not created: $installerPath"
}
Write-Output "Installer ready: $installerPath"
