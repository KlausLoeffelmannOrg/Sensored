$ErrorActionPreference = 'Stop'
$script:RepoRoot = $PSScriptRoot
. (Join-Path $PSScriptRoot 'eng\Build.ps1')

try {
    $options = ConvertTo-SensoredBuildOptions -Arguments $args
    Invoke-SensoredBuild -Options $options
    exit 0
}
catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
