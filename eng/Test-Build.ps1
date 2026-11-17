$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path $PSScriptRoot
. (Join-Path $PSScriptRoot 'Build.ps1')

function Assert-BuildTest {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Assert-BuildThrows {
    param([scriptblock] $Action, [string] $Message)
    $threw = $false
    try { & $Action | Out-Null }
    catch { $threw = $true }
    Assert-BuildTest $threw $Message
}

$defaults = ConvertTo-SensoredBuildOptions @()
Assert-BuildTest ($defaults.Build -and $defaults.Configuration -eq 'Debug' -and
    $defaults.Solution -eq 'Sensored.slnx' -and $defaults.Mode -eq 'package') 'Default must build Debug with packages.'
$project = ConvertTo-SensoredBuildOptions @('-mode', 'project', '-c', 'r', '-platform', 'ARM64', '-test')
Assert-BuildTest ($project.Configuration -eq 'Release' -and $project.Platform -eq 'arm64' -and
    $project.Test -and $project.Solution -eq 'Sensored.Dev.slnx') 'Project-mode aliases or solution selection failed.'
$dev = ConvertTo-SensoredBuildOptions @('-solution', 'Sensored.Dev.slnx')
Assert-BuildTest ($dev.Mode -eq 'project') 'The development solution must select source references.'
$clean = ConvertTo-SensoredBuildOptions @('-clean', '-purgeJsonLocks')
Assert-BuildTest ($clean.Clean -and $clean.PurgeJsonLocks -and -not $clean.Build) 'Standalone clean must not build.'
$reuse = ConvertTo-SensoredBuildOptions @('-test', '-noBuild', '-noRestore')
Assert-BuildTest ($reuse.Test -and $reuse.NoBuild -and $reuse.NoRestore) 'Reusing test binaries must be supported.'
foreach ($arguments in @(
    @('-purgeJsonLocks'), @('-restore', '-test'), @('-restore', '-noRestore'),
    @('-clean', '-test', '-noBuild'), @('-noBuild'), @('-platform', 'x86'),
    @('-mode', 'invalid'), @('-configuration'), @('-templates'), @('-pack'),
    @('-mode', 'package', '-solution', 'Sensored.Dev.slnx')
)) {
    Assert-BuildThrows { ConvertTo-SensoredBuildOptions $arguments } "Invalid arguments accepted: $arguments"
}
Assert-BuildThrows { Assert-SensoredRepositoryPath (Split-Path $script:RepoRoot) } 'Paths outside this repository must be rejected.'
$help = & (Join-Path $script:RepoRoot 'build.cmd') -help 2>&1
Assert-BuildTest ($LASTEXITCODE -eq 0 -and ($help -join "`n") -match 'Sensored build') 'The command wrapper must show help.'

& {
    function Invoke-SensoredTool {
        param([string[]] $Arguments)
        $script:invocations += ,$Arguments
    }

    $script:invocations = @()
    Invoke-SensoredBuild (ConvertTo-SensoredBuildOptions @('-test', '-platform', 'all', '-ci'))
    $restores = @($script:invocations | Where-Object { $_[0] -eq 'restore' })
    $builds = @($script:invocations | Where-Object { $_[0] -eq 'build' })
    $tests = @($script:invocations | Where-Object { $_[0] -eq 'test' })
    Assert-BuildTest ($restores.Count -eq 2 -and $builds.Count -eq 2 -and $tests.Count -eq 1) 'Both architectures must compile; only native tests may run.'
    Assert-BuildTest (($builds | Where-Object { $_ -contains '--tl:off' }).Count -eq 2) 'CI must disable terminal logging.'
    Assert-BuildTest (($builds | Where-Object { $_ -contains '-p:UsePackageDependency=true' }).Count -eq 2) 'Package builds must select NuGet references explicitly.'
    $script:invocations = @()
    Invoke-SensoredBuild (ConvertTo-SensoredBuildOptions @('-mode', 'project', '-test', '-noBuild'))
    Assert-BuildTest ($script:invocations.Count -eq 1 -and $script:invocations[0][0] -eq 'test' -and
        $script:invocations[0] -contains '-p:UsePackageDependency=false') 'noBuild must only test with the selected reference mode.'
}

$repository = $script:RepoRoot
$fixture = Join-Path $repository ('artifacts\build-tests\' + [Guid]::NewGuid().ToString('N'))
try {
    foreach ($directory in @('src\App\bin', 'src\App\obj', 'artifacts\bin\App', 'artifacts\obj\App',
        'artifacts\package', 'artifacts\logs', 'src\ReferenceData')) {
        New-Item -ItemType Directory -Path (Join-Path $fixture $directory) -Force | Out-Null
    }
    Set-Content -LiteralPath (Join-Path $fixture 'src\App\App.csproj') '<Project />'
    Set-Content -LiteralPath (Join-Path $fixture 'src\App\packages.lock.json') '{}'
    Set-Content -LiteralPath (Join-Path $fixture 'src\ReferenceData\recording.csv') 'fixture'
    $script:RepoRoot = $fixture
    Remove-SensoredBuildOutputs $false
    foreach ($deleted in @('src\App\bin', 'src\App\obj', 'artifacts\bin', 'artifacts\obj')) {
        Assert-BuildTest (-not (Test-Path -LiteralPath (Join-Path $fixture $deleted))) "Clean retained $deleted."
    }
    foreach ($preserved in @('artifacts\package', 'artifacts\logs', 'src\ReferenceData\recording.csv', 'src\App\packages.lock.json')) {
        Assert-BuildTest (Test-Path -LiteralPath (Join-Path $fixture $preserved)) "Clean removed $preserved."
    }
    Remove-SensoredBuildOutputs $true
    Assert-BuildTest (-not (Test-Path -LiteralPath (Join-Path $fixture 'src\App\packages.lock.json'))) 'Explicit lock purge must remove locks.'
}
finally {
    $script:RepoRoot = $repository
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}

Write-Host 'Build CLI tests passed.'
