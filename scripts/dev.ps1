param(
    [ValidateSet('Restore', 'Build', 'Test', 'Samples', 'Run', 'Publish')]
    [string]$Task = 'Build'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$sdkExe = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $sdkExe)) { throw 'Install the pinned SDK into .tools/dotnet; see docs/development.md.' }
$env:DOTNET_ROOT = Split-Path $sdkExe -Parent
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.nuget/packages'
Push-Location $repoRoot
try {
    $buildRevision = 'uncommitted'
    $gitHead = & git rev-parse HEAD 2>$null
    if ($LASTEXITCODE -eq 0) {
        $gitChanges = & git status --porcelain
        $buildRevision = if ($gitChanges) { "$gitHead-uncommitted" } else { $gitHead }
    }
    switch ($Task) {
        'Restore' { & $sdkExe restore Skyline.sln --configfile NuGet.Config --locked-mode }
        'Build' { & $sdkExe build Skyline.sln -c Release --no-restore "-p:BuildRevision=$buildRevision" }
        'Test' {
            & $sdkExe run --project tests/Skyline.Infrastructure.Tests -c Release --no-build -- $repoRoot
            if ($LASTEXITCODE -ne 0) { throw 'Template acceptance tests failed.' }
            & $sdkExe run --project tests/Skyline.App.Tests -c Release --no-build
            if ($LASTEXITCODE -ne 0) { throw 'App acceptance tests failed.' }
            & $sdkExe run --project tests/Skyline.Core.Tests -c Release --no-build
        }
        'Samples' { & $sdkExe run --project tests/Skyline.Core.Tests -c Release --no-build -- --samples artifacts/S02/samples }
        'Run' { & $sdkExe run --project src/Skyline.App -c Release --no-build }
        'Publish' { & $sdkExe publish src/Skyline.App -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true -p:RestoreConfigFile=NuGet.Config "-p:BuildRevision=$buildRevision" -o artifacts/S02/win-x64 }
    }
    if ($LASTEXITCODE -ne 0) { throw "dotnet $Task failed with exit code $LASTEXITCODE" }
}
finally { Pop-Location }
