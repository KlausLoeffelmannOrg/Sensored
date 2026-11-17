function ConvertTo-SensoredBuildOptions {
    param([string[]] $Arguments)

    $options = @{
        Configuration = 'Debug'; Platform = ''; Solution = ''; Mode = ''
        Clean = $false; Build = $false; Test = $false; PurgeJsonLocks = $false
        NoBuild = $false; NoRestore = $false; RestoreOnly = $false; Ci = $false
        Help = $false; WarpPackageVersion = ''
    }

    for ($i = 0; $i -lt $Arguments.Count; $i++) {
        $argument = $Arguments[$i].ToLowerInvariant()
        switch ($argument) {
            { $_ -in '-configuration', '-c', '-platform', '-solution', '-mode', '-warppackageversion' } {
                if (++$i -ge $Arguments.Count -or $Arguments[$i].StartsWith('-')) {
                    throw "Missing value for $argument."
                }
                $value = $Arguments[$i]
                switch ($argument) {
                    { $_ -in '-configuration', '-c' } {
                        switch ($value.ToLowerInvariant()) {
                            { $_ -in 'd', 'debug' } { $options.Configuration = 'Debug' }
                            { $_ -in 'r', 'release' } { $options.Configuration = 'Release' }
                            default { throw "Unsupported configuration '$value'; use Debug or Release." }
                        }
                    }
                    '-platform' {
                        if ($value.ToLowerInvariant() -notin 'x64', 'arm64', 'all') {
                            throw "Unsupported platform '$value'; use x64, arm64 or all."
                        }
                        $options.Platform = $value.ToLowerInvariant()
                    }
                    '-solution' { $options.Solution = $value }
                    '-mode' {
                        if ($value.ToLowerInvariant() -notin 'package', 'project') {
                            throw "Unsupported mode '$value'; use package or project."
                        }
                        $options.Mode = $value.ToLowerInvariant()
                    }
                    '-warppackageversion' { $options.WarpPackageVersion = $value }
                }
            }
            '-clean' { $options.Clean = $true }
            '-purgejsonlocks' { $options.PurgeJsonLocks = $true }
            '-build' { $options.Build = $true }
            '-test' { $options.Test = $true }
            '-nobuild' { $options.NoBuild = $true }
            '-norestore' { $options.NoRestore = $true }
            '-restore' { $options.RestoreOnly = $true }
            '-ci' { $options.Ci = $true }
            { $_ -in '-help', '-h', '-?' } { $options.Help = $true }
            default { throw "Unknown build argument '$($Arguments[$i])'. Run build -help." }
        }
    }

    if ($options.Help) { return $options }
    if ($options.PurgeJsonLocks -and -not $options.Clean) { throw '-purgeJsonLocks requires -clean.' }
    if ($options.RestoreOnly -and ($options.NoRestore -or $options.NoBuild -or $options.Clean -or $options.Build -or $options.Test)) {
        throw '-restore is a standalone restore-only operation.'
    }
    if ($options.NoBuild -and ($options.Clean -or $options.Build -or -not $options.Test)) {
        throw '-noBuild requires -test and cannot be combined with -clean or -build.'
    }
    if (-not ($options.Clean -or $options.Build -or $options.Test -or $options.RestoreOnly)) {
        $options.Build = $true
    }
    if (-not $options.Solution) {
        $options.Solution = if ($options.Mode -eq 'project') { 'Sensored.Dev.slnx' } else { 'Sensored.slnx' }
    }
    $isDev = [IO.Path]::GetFileName($options.Solution) -ieq 'Sensored.Dev.slnx'
    if (-not $options.Mode) { $options.Mode = if ($isDev) { 'project' } else { 'package' } }
    if ($isDev -and $options.Mode -eq 'package') {
        throw 'Sensored.Dev.slnx contains WARP projects; use Sensored.slnx for package mode.'
    }

    return $options
}

function Assert-SensoredRepositoryPath {
    param([string] $Path)

    $root = [IO.Path]::GetFullPath($script:RepoRoot).TrimEnd('\') + '\'
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the repository: $full"
    }
    $current = $full
    while ($current.Length -ge $root.Length) {
        if ((Test-Path -LiteralPath $current) -and
            ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing to follow a reparse point: $current"
        }
        $current = Split-Path $current
    }

    return $full
}

function Remove-SensoredBuildOutputs {
    param([bool] $PurgeJsonLocks)

    $paths = @(
        (Join-Path $script:RepoRoot 'artifacts\bin'),
        (Join-Path $script:RepoRoot 'artifacts\obj')
    )
    $projects = Get-ChildItem -LiteralPath (Join-Path $script:RepoRoot 'src') -Recurse -Filter '*.csproj' -File |
        Where-Object { $_.FullName.Substring($script:RepoRoot.Length) -notmatch '\\(bin|obj|artifacts|\.vs)\\' }
    foreach ($project in $projects) {
        $paths += Join-Path $project.DirectoryName 'bin'
        $paths += Join-Path $project.DirectoryName 'obj'
    }
    if ($PurgeJsonLocks) {
        $paths += Get-ChildItem -LiteralPath (Join-Path $script:RepoRoot 'src') -Recurse -Filter 'packages.lock.json' -File |
            Select-Object -ExpandProperty FullName
    }
    foreach ($path in $paths) {
        $resolved = Assert-SensoredRepositoryPath $path
        if (-not (Test-Path -LiteralPath $resolved)) { continue }
        $reparse = Get-ChildItem -LiteralPath $resolved -Recurse -Force |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1
        if ($reparse) { throw "Refusing to clean through reparse point '$($reparse.FullName)'." }
        Write-Host "Deleting build output: $resolved"
        Remove-Item -LiteralPath $resolved -Recurse -Force
        if (Test-Path -LiteralPath $resolved) { throw "Build output still exists after clean: $resolved" }
    }
}

function Invoke-SensoredTool {
    param([string[]] $Arguments)

    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE." }
}

function Show-SensoredBuildHelp {
    @'
Sensored build (Windows 11 build 22000+, .NET 11 SDK)
  build                              Restore/build Debug using WARP NuGets
  build -mode project                Build Sensored.Dev.slnx with sibling WARP sources
  build -c r [-platform x64|arm64|all]
  build -clean                       Purge this repo's bin/obj, not WARP outputs
  build -clean -purgeJsonLocks        Also delete packages.lock.json under src
  build -clean -build                Purge then compile
  build -test                        Build/run both app test suites on native architecture
  build -restore                     Restore only
  -noRestore                         Require existing matching restore assets
  -noBuild                           With -test: reuse existing binaries
  -solution path                     Opt-in repository solution/project
  -mode package|project              Override WARP dependency mode
  -warpPackageVersion version        Override the pinned WARP NuGet version
  -ci                                Disable terminal logger
Outputs: artifacts\bin, obj, logs, TestResults; dependency modes/architectures are isolated.
No toolkit NuGet packing, templates, API-doc generation, publishing or automatic Git operations.
'@ | Write-Host
}

function Invoke-SensoredBuild {
    param([hashtable] $Options)

    if ($Options.Help) { Show-SensoredBuildHelp; return }
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or [Environment]::OSVersion.Version.Build -lt 22000) {
        throw 'Sensored build supports Windows build 22000 or later only.'
    }
    $native = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    if ($native -notin 'x64', 'arm64') { throw "Unsupported native architecture '$native'." }
    if (-not $Options.Platform) { $Options.Platform = $native }
    $architectures = @($Options.Platform)
    if ($Options.Platform -eq 'all') { $architectures = @('x64', 'arm64') }
    $solutionPath = $Options.Solution
    if (-not [IO.Path]::IsPathRooted($solutionPath)) { $solutionPath = Join-Path $script:RepoRoot $solutionPath }
    $solution = Assert-SensoredRepositoryPath $solutionPath
    if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) { throw "Solution/project not found: $solution" }
    if ($Options.Clean) { Remove-SensoredBuildOutputs $Options.PurgeJsonLocks }
    $requiresInputs = $Options.Build -or $Options.Test
    $terminalLogger = '--tl:on'
    if ($Options.Ci -or $env:CI -eq 'true' -or $env:GITHUB_ACTIONS -eq 'true') { $terminalLogger = '--tl:off' }
    $logs = Join-Path $script:RepoRoot 'artifacts\logs'
    if ($requiresInputs -or $Options.RestoreOnly) { New-Item -ItemType Directory -Path $logs -Force | Out-Null }
    $usePackages = if ($Options.Mode -eq 'package') { 'true' } else { 'false' }

    Push-Location $script:RepoRoot
    try {
        foreach ($architecture in $architectures) {
            $properties = @("-p:WarpBuildArchitecture=$architecture", "-p:Configuration=$($Options.Configuration)",
                "-p:UsePackageDependency=$usePackages", '-p:GenerateApiDocs=false')
            if ($Options.WarpPackageVersion) { $properties += "-p:WarpPackageVersion=$($Options.WarpPackageVersion)" }
            if ($Options.RestoreOnly -or ($requiresInputs -and -not $Options.NoBuild -and -not $Options.NoRestore)) {
                Invoke-SensoredTool (@('restore', $solution) + $properties)
            }
            if ($requiresInputs -and -not $Options.NoBuild) {
                $binlog = Join-Path $logs "build-$($Options.Mode)-$($Options.Configuration)-$architecture.binlog"
                Invoke-SensoredTool (@('build', $solution, '--no-restore', '-m:1', $terminalLogger, '-v:minimal', "-bl:$binlog") + $properties)
            }
            if ($Options.Test) {
                if ($architecture -ne $native) {
                    Write-Warning "Built $architecture test inputs; runtime tests require a native $architecture machine and were not run."
                    continue
                }
                $results = Join-Path $script:RepoRoot "artifacts\TestResults\$($Options.Mode)\$($Options.Configuration)\$architecture"
                Invoke-SensoredTool (@('test', $solution, '--no-build', '--no-restore', '-m:1',
                    '-p:TestTfmsInParallel=false', '--logger', 'console;verbosity=minimal',
                    '--results-directory', $results) + $properties)
            }
        }
    }
    finally {
        Pop-Location
    }
}
