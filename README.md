# Sensored

The home of **Sensored**, **FusionGauges**, and the **WarpSensorsDataProcessor**
recording-analysis CLI, moved from WARP without importing its Git history.
Both applications are experimental driving diagnostics, not safety-critical instruments.
Configure sources and operate the UI only while parked or through a passenger.

## Solutions

Requires Windows 11 (build 22000+) and the .NET 11 SDK with Windows Forms support.

| Solution | WARP dependencies |
| --- | --- |
| `Sensored.slnx` | Pinned NuGet packages; no WARP checkout required |
| `Sensored.Dev.slnx` | Project references to `..\WARP\src` |

Both solutions group the apps and their tests under **App**. The development solution's
**WARP** folder contains the library dependency graph; it is empty in the NuGet solution.
Opening the development solution selects project mode automatically. Building an individual
project defaults to packages; pass `-p:UsePackageDependency=false` to use WARP sources.
`WarpRoot` can override the source location for individual projects or the app-only solution;
the development solution's visible library entries require the sibling layout.

The package version is pinned centrally by `WarpPackageVersion` in `Directory.Build.props`.
**Package release prerequisite:** the current apps require APIs newer than the latest
published WARP packages (`0.9.852-preview.g888af68ad8`). The pin identifies the WARP
`e4bd1a23` source snapshot. NuGet mode requires matching packages to be published
or packed locally and restored from a local feed; it never silently falls back to sources.
Until that release is available, use `.\build.cmd -mode project`.
`NuGet.config` uses the public Microsoft NuGet mirror and .NET feeds, matching WARP.

## Build

From this repository root in PowerShell:

```powershell
.\build.cmd
.\build.cmd -mode project -test
.\build.cmd -c r -platform all
.\build.cmd -solution Sensored.Dev.slnx -test
.\build.cmd -restore
.\build.cmd -test -noBuild
.\build.cmd -clean -build
.\build.cmd -help
```

WARP's applicable CLI options are retained: `-configuration`/`-c` (`d`/`r` aliases),
`-platform x64|arm64|all`, `-clean`, `-purgeJsonLocks`, `-build`, `-test`, `-restore`,
`-noRestore`, `-noBuild`, `-solution`, `-ci`, and `-help`/`-h`/`-?`.
`-mode package|project` and `-warpPackageVersion version` select dependencies.
Toolkit packing/versioning, templates, and API-documentation/site publishing are omitted.

Outputs are under `artifacts\bin`, `artifacts\obj`, `artifacts\logs`, and
`artifacts\TestResults`, isolated by dependency mode and architecture. Clean only removes
this repository's build outputs, never those of the sibling WARP checkout.
Cross-architecture builds compile tests but only run them on the native architecture.

To validate package mode before a matching WARP release, build and pack that WARP
snapshot locally, then restore the app-only solution from those packages:

```powershell
.\build.cmd -mode project
$architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$packages = Join-Path $PWD 'artifacts\package'
[xml]$solution = Get-Content .\Sensored.Dev.slnx -Raw
foreach ($project in $solution.SelectNodes('//Folder[@Name="/WARP/"]/Project')) {
    dotnet pack $project.Path -c Debug --no-build --no-restore -o $packages `
      "-p:WarpBuildArchitecture=$architecture" -p:UsePackageDependency=false `
      -p:PackageVersion=0.11.0-preview.ge4bd1a23
    if ($LASTEXITCODE -ne 0) { throw "Packing failed: $($project.Path)" }
}
dotnet restore Sensored.slnx "-p:RestoreAdditionalProjectSources=$PWD\artifacts\package"
.\build.cmd -test -noRestore
```

## Tests

`.\build.cmd -test` runs both test suites; `pwsh -File .\eng\Test-Build.ps1`
checks CLI parsing, operation selection, architecture handling, and safe cleaning.
The unchanged two-case
`FusionSetupAppTests.SensoredDrivingTasksRecordIntentBoundariesWithoutChangingAcquisitionOrFusion`
currently hangs. It is not silently excluded by the CLI. To run the remaining 275
tests explicitly while that existing issue is investigated:

```powershell
dotnet test Sensored.Dev.slnx --no-build --no-restore -m:1 `
  --filter "FullyQualifiedName!~SensoredDrivingTasksRecordIntentBoundariesWithoutChangingAcquisitionOrFusion"
```

## Apps and fixtures

- [Sensored and processor documentation](src/Sensored/README.md)
- [FusionGauges documentation](src/FusionGauges/README.md)
- [Fusion reference experiments](src/Sensored/FusionReference/NOTES.md)

Real-world recording fixtures remain in the private WARP checkout at
`..\WARP\samples\cs\Sensored\ReferenceData`; they are not published in this public
repository. An optional local copy at `src\Sensored\ReferenceData` is ignored by Git.
The two test projects use synthetic fixtures and do not require those recordings or live sensors.

```powershell
dotnet run --project src\Sensored\Sensored
dotnet run --project src\FusionGauges\WinFormsFusionGauges.WinFormsApp
dotnet run --project src\Sensored\WarpSensorsDataProcessor -- --help
```
