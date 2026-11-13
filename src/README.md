# WARP Sensors Demo

`WarpSensorsDemo` is a thin WinForms client composed from `WarpToolkit.Sensors`,
`WarpToolkit.Windows.Sensors`, `WarpToolkit.Sensors.Presentation`, and
`WarpToolkit.WinForms.Sensors.DirectX`. It demonstrates service-owned sensor execution, independent
async streams, recording, replay-compatible files, and road-section classification without
calibration or corrected/projected sensor values.

## What the demo does

- Starts and stops a shared sensor-service lease.
- Selects Windows IMU/GPS, Windows IMU only, Windows IMU with serial NMEA GPS, or no sources.
- Consumes a declarative `IAsyncEnumerable<SensorEnvelope>` subscription.
- Displays a gauge-only dashboard with raw acceleration, raw inclinometer attitude, compass,
  speed/trip/altitude, a full-height route map with a bounded zoom slider, animated telemetry
  read-outs, road-section classifications, and a Direct2D debug log.
- Starts and stops service-owned recordings in all three formats.
- Includes raw Windows sensor diagnostics and a schema-driven recording viewer.
- Applies optional damping through UI-neutral presentation ViewModels consumed by the DirectX
  controls.

The demo owns no calibration, fusion, trip calculation, classifier, recording writer, replay schema,
or gauge math. Those responsibilities belong to the Core and Presentation libraries.

## Threading

The service creates one dedicated sensor-processing background thread for every active session, even
when only one or no hardware source is selected. Source callbacks enqueue only. The UI consumes its
subscription asynchronously and uses `Control.InvokeAsync` before updating controls.

Slow subscribers do not block sensor processing. Their bounded queues drop the newest item and expose
drop statistics and explicit gap envelopes.

## Values

The dashboard consumes raw IMU envelopes rather than processed motion. It deliberately has no level
reference, guided calibration drive, calibration profile, drift correction, corrected GPS speed, or
projected speed. Raw device X and Y are caller-selected as the displayed forward and lateral axes;
the demo applies no axis correction.

The **Damping** setting is presentation-only. It never changes service envelopes, recorded rows,
replay data, or classifier inputs. **Pause** freezes dashboard visuals only; acquisition and
recording continue.

## Recordings

Recordings are created below:

```text
%LocalAppData%\WARP\WarpSensorsDemo\Recordings
```

Every request creates a separate folder. The service writes a globally ordered event stream in:

- verbose JSON Lines;
- compact schema-header JSON;
- semicolon CSV with invariant numbers and conditional per-field quoting.

Parts split at wall-clock full-minute boundaries or the configured maximum row count. Finalized file
names contain the dominant road-classification abbreviation. Each part has a JSON classification
sidecar, and the folder has a recording manifest.

## Classification

The live classification stage runs independently of recording and publishes classifications from
the fixed taxonomy: parked, straight, sweeping/normal/sharp curves, turns, hairpins, serpentines,
acceleration and hard acceleration, braking and hard braking, rough, unknown, and unusable.
Recording consumes those same results rather than running a second classifier. Results include
confidence and zero-based inclusive session row ranges.

The demo shortens the live classification window to five seconds. The service default closes a
window after 512 observed rows or 30 seconds of source time, which makes a dashboard receiving about
one GPS fix per second look as if its route-section list had stopped updating.

The **Route sections** list shows the symbol and description of each section together with the start
and end odometer reading, their times, and the number of GPS fixes the section was classified from.

## Dashboard layout

The map owns the upper part of the right column and has a vertical zoom slider beside it. The slider
is deliberately limited to two ticks out and four ticks in, relative to the map's automatic route
fit; the chosen offset is restored on the next start. Map rendering debug information is an option
and is off by default.

The lower part of the right column is a `FluentTabControl` with **Telemetry**, **Route sections**
and **Debug** tabs. A toolbar toggle switches it to a side-by-side split view that shows the
telemetry group boxes next to the route sections and the debug log; the chosen mode is restored on
the next start.

Telemetry is grouped into **GPS**, **Artificial Horizon** and **Acceleration Input** group boxes.
Each group's read-outs share one `ValueLabelLayoutManager`, so every value in a group renders against
the same decimal-point column, and the animated indicator bars show attack and release inertia, the
recent value history, the all-time maximum and the running-average band.

Informational, warning and error messages are written to the Direct2D console in the **Debug** tab.
The structured record of failures stays in the exception history reachable from the status bar.

## Packaging

The project remains a single-project MSIX application so Windows can grant consent-gated location
capabilities declared in `Package.appxmanifest`. An unpackaged Debug build still runs, but Windows
location access may be unavailable depending on the host and privacy settings.

Motion sensors from `Windows.Devices.Sensors` require no package capability.

## Dependencies and build

The demo consumes the WARP libraries as NuGet packages pinned to
`0.9.491-preview.g9be8b31731`; it has no project references to the WARP repository.
Repository-local `NuGet.config` clears inherited package sources and uses only the approved
`nuget-public`, `dotnet11`, and `dotnet10` Azure Artifacts feeds first. NuGet.org V3 is temporarily
configured as the final fallback until the latest WARP packages are mirrored by `nuget-public`.

Restore and build from the repository root:

```powershell
dotnet restore src\WarpSensorsDemo.sln
dotnet build src\WarpSensorsDemo.sln --no-restore
```

## Diagnostics

Unhandled startup, UI, background, and task exceptions are written below:

```text
%LocalAppData%\WARP\WarpSensorsDemo\CrashReports
```

Each failure produces a timestamped `warp-crash-*.txt` report with the exception and startup stage.
`warp-lifecycle.jsonl` records the corresponding process lifecycle and failure events.

## Deterministic validation

The library and demo are validated through fake sources, replay, and file-I/O tests. No physical
sensor array is required. During active Release use, build and test the Debug configuration only.
