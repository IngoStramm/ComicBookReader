param(
    [string]$DotNet = (Join-Path $env:LOCALAPPDATA 'ComicBookReader-dev/dotnet-10.0.401/dotnet.exe'),
    [string]$Iscc = (Join-Path $env:LOCALAPPDATA 'ComicBookReader-dev/inno-6.7.3/ISCC.exe')
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packageDirectory = Join-Path $projectRoot 'artifacts/win-x64-distribution'
$workDirectory = Join-Path $projectRoot 'artifacts/release-work'
$outputDirectory = Join-Path $projectRoot 'artifacts/distribution'
[IO.Directory]::CreateDirectory($workDirectory) | Out-Null
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
Push-Location $projectRoot
try {
    & (Join-Path $PSScriptRoot 'publish.ps1') -DotNet $DotNet -OutputDirectory 'artifacts/win-x64-distribution'
    $texts = [Text.StringBuilder]::new()
    [void]$texts.AppendLine([IO.File]::ReadAllText((Join-Path $projectRoot 'THIRD-PARTY-NOTICES.md')))
    foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $packageDirectory 'licenses') -File | Sort-Object Name)) {
        [void]$texts.AppendLine()
        [void]$texts.AppendLine("===== $($file.Name) =====")
        [void]$texts.AppendLine([IO.File]::ReadAllText($file.FullName))
    }
    $licensesPath = Join-Path $workDirectory 'ThirdPartyLicenses.txt'
    [IO.File]::WriteAllText($licensesPath, $texts.ToString(), [Text.UTF8Encoding]::new($false))
    & (Join-Path $PSScriptRoot 'publish.ps1') -DotNet $DotNet -OutputDirectory 'artifacts/win-x64-distribution' -ThirdPartyLicensesFile $licensesPath
    $portableDirectory = Join-Path $workDirectory ('portable-' + [Guid]::NewGuid().ToString('N'))
    rtk proxy $DotNet publish src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Release --runtime win-x64 --self-contained true --output $portableDirectory -p:RestoreLockedMode=true -p:BuildPortable=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -p:DebugSymbols=false "-p:ThirdPartyLicensesFile=$licensesPath"
    if ($LASTEXITCODE -ne 0) { throw 'Single-file publication failed.' }
    $executable = Join-Path $portableDirectory 'ComicBookReader.App.exe'
    if (-not (Test-Path -LiteralPath $executable)) { throw 'Missing single-file executable.' }
    $sidecars = @(Get-ChildItem -LiteralPath $portableDirectory -File | Where-Object Name -ne 'ComicBookReader.App.exe')
    if ($sidecars.Count -ne 0) { throw 'Single-file publication produced unexpected sidecar files.' }
    $versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($executable)
    $version = '{0}.{1}.{2}' -f $versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart
    $portablePath = Join-Path $outputDirectory "ComicBookReader-$version-Portable.exe"
    Copy-Item -LiteralPath $executable -Destination $portablePath
    & (Join-Path $PSScriptRoot 'build-installer.ps1') -Iscc $Iscc
    $installerPath = Join-Path $outputDirectory "ComicBookReader-$version-Setup.exe"
    $hashes = foreach ($filePath in @($portablePath, $installerPath)) {
        $hash = Get-FileHash -LiteralPath $filePath -Algorithm SHA256
        '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), [IO.Path]::GetFileName($filePath)
    }
    [IO.File]::WriteAllLines((Join-Path $outputDirectory "SHA256SUMS-$version.txt"), $hashes, [Text.UTF8Encoding]::new($false))
    $instructions = @"
ComicBookReader $version - Windows x64

Português (Brasil)
Portátil: abra ComicBookReader-$version-Portable.exe. Basta compartilhar este arquivo.
Instalador: abra ComicBookReader-$version-Setup.exe. Instala para seu usuário, com atalho no menu Iniciar, opção de atalho na área de trabalho e desinstalação. Na instalação, você pode abrir as Configurações do Windows para escolher o ComicBookReader como padrão para arquivos .cbz e .cbr.
O .NET está incluído. Licenças completas: botão Sobre na barra do leitor.
Preferências e progresso são mantidos em %LOCALAPPDATA%\ComicBookReader, inclusive ao desinstalar.

English
Portable: run ComicBookReader-$version-Portable.exe. Only this file needs to be shared.
Installer: run ComicBookReader-$version-Setup.exe. Installs for your user, with a Start menu shortcut, optional desktop shortcut and uninstaller. During setup, you can open Windows Settings to choose ComicBookReader as the default for .cbz and .cbr files.
.NET is included. Full third-party licenses: About button in the reader toolbar.
Preferences and reading progress remain in %LOCALAPPDATA%\ComicBookReader, including after uninstall.
"@
    [IO.File]::WriteAllText((Join-Path $outputDirectory "README-$version.txt"), $instructions, [Text.UTF8Encoding]::new($false))
    Write-Output "Distribution ready: $portablePath and $installerPath"
} finally { Pop-Location }
