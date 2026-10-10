param(
    [string]$Version = '0.1.0'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$distribution = Join-Path $projectRoot 'artifacts/distribution'
$portable = Join-Path $distribution "ComicBookReader-$Version-Portable.exe"
$installer = Join-Path $distribution "ComicBookReader-$Version-Setup.exe"
$licenses = Join-Path $projectRoot 'artifacts/release-work/ThirdPartyLicenses.txt'
$runId = [Guid]::NewGuid().ToString('N')
$evidenceRelative = "artifacts/distribution-validation/$runId"
$evidence = Join-Path $projectRoot $evidenceRelative
$installDirectory = Join-Path $evidence 'install with spaces'
$keyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{3E53FBD8-7CF4-4E2D-9353-1F8DD2A7651C}_is1'
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'ComicBookReader.lnk'
$group = "ComicBookReader Validation $runId"
$menuLink = Join-Path ([Environment]::GetFolderPath('Programs')) "$group/ComicBookReader.lnk"
foreach ($file in @($portable, $installer, $licenses)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing distribution input: $file" }
}
if (Test-Path -LiteralPath $keyPath) { throw 'An installed ComicBookReader already exists; validation must not overwrite it.' }
if (Test-Path -LiteralPath $desktopLink) { throw 'An existing desktop shortcut must not be overwritten by validation.' }
[IO.Directory]::CreateDirectory($evidence) | Out-Null

function Run-SetupProcess([string]$ExecutablePath, [string[]]$Arguments) {
    $process = Start-Process -FilePath $ExecutablePath -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { throw "Setup process timed out: $ExecutablePath" }
    $process.Refresh()
    if ($process.ExitCode -ne 0) { throw "Setup process returned $($process.ExitCode): $ExecutablePath" }
}

function Uninstall-OwnedTest {
    if (-not (Test-Path -LiteralPath $keyPath)) { return }
    $registered = (Get-ItemProperty -LiteralPath $keyPath).InstallLocation
    $expected = [IO.Path]::GetFullPath($installDirectory).TrimEnd('\')
    if ([IO.Path]::GetFullPath($registered).TrimEnd('\') -ne $expected -or
        -not $expected.StartsWith([IO.Path]::GetFullPath($evidence) + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to uninstall a path outside this validation run.'
    }
    Run-SetupProcess (Join-Path $expected 'unins000.exe') @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$(Join-Path $evidence 'uninstall.log')`"")
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while ((Test-Path -LiteralPath $keyPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (Test-Path -LiteralPath $keyPath) { throw 'Uninstall registration remained after uninstall.' }
}

$preferencesDirectory = Join-Path $env:LOCALAPPDATA 'ComicBookReader'
$preferencesPath = Join-Path $preferencesDirectory 'preferences.json'
$preferencesHash = if (Test-Path -LiteralPath $preferencesPath) { (Get-FileHash -LiteralPath $preferencesPath).Hash } else { $null }
[IO.Directory]::CreateDirectory($preferencesDirectory) | Out-Null
$marker = Join-Path $preferencesDirectory "distribution-validation-$runId.txt"
[IO.File]::WriteAllText($marker, $runId)
try {
    $portableOnly = Join-Path $evidence 'portable only'
    [IO.Directory]::CreateDirectory($portableOnly) | Out-Null
    $standalone = Join-Path $portableOnly 'Reader standalone.exe'
    Copy-Item -LiteralPath $portable -Destination $standalone
    if (@(Get-ChildItem -LiteralPath $portableOnly -Force).Count -ne 1) { throw 'Portable validation directory is not isolated.' }
    & (Join-Path $PSScriptRoot 'verify-app.ps1') -Executable "$evidenceRelative/portable only/Reader standalone.exe" -EvidenceDirectory "$evidenceRelative/portable-runtime" -Standalone -BundledLicensesFile $licenses
    Write-Output 'PASS portable: renamed EXE alone, path with spaces, included runtime and full licenses'

    $commonArguments = @('/CURRENTUSER', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOCLOSEAPPLICATIONS', '/NORESTARTAPPLICATIONS', "/DIR=`"$installDirectory`"", "/GROUP=`"$group`"", '/TASKS=desktopicon')
    Run-SetupProcess $installer ($commonArguments + @('/LANG=brazilianportuguese', "/LOG=`"$(Join-Path $evidence 'install.log')`""))
    $registration = Get-ItemProperty -LiteralPath $keyPath
    if ($registration.DisplayVersion -ne $Version) { throw 'Installed version metadata mismatch.' }
    foreach ($file in @('ComicBookReader.App.exe', 'libSkiaSharp.dll', 'unins000.exe', 'licenses/SkiaSharp-LICENSE.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $installDirectory $file))) { throw "Installer missed required file: $file" }
    }
    if (@(Get-ChildItem -LiteralPath $installDirectory -Filter '*.pdb' -Recurse).Count -ne 0) { throw 'Installer included debug symbols.' }
    $shell = New-Object -ComObject WScript.Shell
    foreach ($link in @($desktopLink, $menuLink)) {
        if (-not (Test-Path -LiteralPath $link)) { throw "Missing shortcut: $link" }
        if ($shell.CreateShortcut($link).TargetPath -ne (Join-Path $installDirectory 'ComicBookReader.App.exe')) { throw "Wrong shortcut target: $link" }
    }
    $retainedFile = Join-Path $installDirectory 'user-content-validation.txt'
    [IO.File]::WriteAllText($retainedFile, $runId)
    Run-SetupProcess $installer ($commonArguments + @('/LANG=english', "/LOG=`"$(Join-Path $evidence 'reinstall.log')`""))
    if ([IO.File]::ReadAllText($retainedFile) -ne $runId) { throw 'Reinstall changed user content.' }
    & (Join-Path $PSScriptRoot 'verify-app.ps1') -Executable "$evidenceRelative/install with spaces/ComicBookReader.App.exe" -EvidenceDirectory "$evidenceRelative/installed-runtime" -BundledLicensesFile $licenses
    Write-Output 'PASS installer: per-user install, both languages, shortcuts, reinstall, runtime and full licenses'
    Uninstall-OwnedTest
    foreach ($file in @((Join-Path $installDirectory 'ComicBookReader.App.exe'), $desktopLink, $menuLink)) {
        if (Test-Path -LiteralPath $file) { throw "Installed item remained after uninstall: $file" }
    }
    if ([IO.File]::ReadAllText($retainedFile) -ne $runId -or [IO.File]::ReadAllText($marker) -ne $runId) { throw 'Uninstall removed user content.' }
    if ($preferencesHash -and (Get-FileHash -LiteralPath $preferencesPath).Hash -ne $preferencesHash) { throw 'Real preferences changed during validation.' }
    if (-not $preferencesHash -and (Test-Path -LiteralPath $preferencesPath)) { throw 'Validation created real preferences.' }
    $result = [ordered]@{
        Version = $Version
        PortableSHA256 = (Get-FileHash -LiteralPath $portable).Hash
        InstallerSHA256 = (Get-FileHash -LiteralPath $installer).Hash
        PortableAlone = $true
        IncludedRuntime = $true
        FullLicenses = $true
        InstallReinstallUninstall = $true
        Languages = @('pt-BR', 'en')
        UserDataPreserved = $true
    }
    [IO.File]::WriteAllText((Join-Path $evidence 'result.json'), ($result | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Output "PASS uninstall: shortcuts/registration removed, preferences and user content preserved; evidence: $evidence"
} finally {
    Uninstall-OwnedTest
    if ((Test-Path -LiteralPath $marker) -and [IO.File]::ReadAllText($marker) -eq $runId) { Remove-Item -LiteralPath $marker }
}
