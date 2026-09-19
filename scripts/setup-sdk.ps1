# Installs only an isolated SDK under this checkout, never changes machine PATH.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$sdkVersion = (Get-Content (Join-Path $repoRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
if ($sdkVersion -ne '10.0.401') { throw 'Update this installer hash when changing global.json.' }
$expectedHash = '24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430'
$toolRoot = Join-Path $repoRoot '.tools'
$sdkRoot = Join-Path $toolRoot 'dotnet'
$sdkExe = Join-Path $sdkRoot 'dotnet.exe'
if (Test-Path -LiteralPath $sdkExe) {
    $installed = & $sdkExe --list-sdks
    if ($LASTEXITCODE -ne 0 -or -not ($installed -match '^10\.0\.401 ')) { throw 'Existing local SDK does not match the pinned version.' }
    Write-Output 'Pinned SDK already present.'
    return
}
New-Item -ItemType Directory -Force $toolRoot | Out-Null
$archive = Join-Path $toolRoot 'dotnet-sdk.zip'
if (-not (Test-Path -LiteralPath $archive) -or (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $expectedHash) {
    Invoke-WebRequest "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-win-x64.zip" -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $expectedHash) { throw 'SDK SHA-512 check failed.' }
Expand-Archive -LiteralPath $archive -DestinationPath $sdkRoot -Force
& $sdkExe --list-sdks
if ($LASTEXITCODE -ne 0) { throw 'SDK installation verification failed.' }
