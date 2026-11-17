# FusionGauges: first iteration

A .NET 11 WinForms driving-diagnostics sample using WARP acquisition, causal pattern fusion,
measured mounting references, four DirectX gauges, and the existing raw recorder.
Configure sources, storage, and calibration **while parked**. The application is
diagnostic software, not a safety-critical driving instrument.

## Build and run

Requires Windows and the .NET 11 SDK with WinForms support. From the repository root:

```powershell
.\build.cmd -mode project
dotnet run --project src\FusionGauges\WinFormsFusionGauges.WinFormsApp -p:UsePackageDependency=false
```

The root `Sensored.Dev.slnx` groups source ProjectReferences under `WARP`.
Use `Sensored.slnx` (or `.\build.cmd` without `-mode project`) for WARP NuGet
dependencies instead. Both solutions include Sensored and both app test suites.

## Using the cockpit

New recordings always include `Filename.RawV2.csv`, `Filename.FuseV2.csv` and a
mandatory full-rate compact-JSON replay journal. **Tools / Options / Recording**
sets the RawV2 rate from 1 to 20 snapshots/s, default 10; changes apply to the next
recording. Fusion stays full-rate and includes distinct unblended OBD/GPS speed
columns. Original per-metric timestamps, source generations and stale/unavailable
states remain visible; unsupported vehicle PIDs are not invented.
Legacy envelope CSV/JSON imports remain supported, but new sessions do not write
the old envelope CSV. Completed ZIP mirrors include the pair, markers and terminal
`v2.manifest.json`. An incomplete pair never claims successful journal coverage.
The existing **Marker / Ctrl+M** command also writes V2 markers; its configured
label can contain a note. Enter labels only while parked or through a passenger.

The shell contains a MenuStrip, large icon-and-text ToolStrip, Fluent tab content, and
StatusStrip. Toolbar order is **Start/Stop sensors**, **Start/Stop recording**, **Marker**,
**Instruments**, **Diagnostics**, **Log**, **Retry ZIPs**, **Fusion setup**, **Options**,
**Speed source**, **Full screen/Windowed**, **Special**, **Clipboard**, and **Exit**. Separators group
those commands. **Instruments**, **Diagnostics** and **Log** replace the built-in tab
header to reclaim its height; the selected button stays checked. The recording page
button is named Log; it is the same `_recordingTabButton`.
The shared `FluentTabControl.BindNavigationButton(button, tabContent)` API binds
caller-owned buttons on any external ToolStrip. `ShowTabHeaders = false` hides only
the internal header; selection, tab-change events, options and programmatic switching remain available.
`UnbindNavigationButton` releases an individual binding; disposal also detaches handlers.
Instruments places the speedometer above acceleration on the left,
the artificial horizon above the compass on the right, and scrollable telemetry
groups in between. Diagnostics keeps raw device-frame readings visibly separate
from calibrated vehicle-frame outputs. Recording shows local output and ZIP backlog.
The upper instrument row is 20% taller than the lower row (6:5).

1. Open **Tools / Options** while parked and select sources and storage.
2. Opening the application starts GPS-only preview and a bounded background search
   for the already-paired adapter selected in **Options → Sources / GPS → Paired OBD2 adapter**
   (OBDLink CX by default, or AD20-13303). **Start sensors** additionally starts drive
   IMU acquisition, fusion and optional automatic recording.
3. Open **Fusion setup** to inspect live per-quantity support, exact gate reasons and
   next actions. This never starts or restarts sensors. The instrument banner remains
   visible even when raw acquisition is healthy but horizon/vehicle acceleration are
   unavailable. With acquisition and recording stopped, the **Measured physical mount**
   tab accepts independently measured vehicle-forward/down unit axes in device
   coordinates, identity, revision and provenance/uncertainty notes. It does not guess
   axes or turn gravity into vehicle down. Save the profile for the next live session;
   moving the device requires re-measurement and a new revision or identity.
   **Tools / Landscape motion calibration** remains a separate diagnostic aid.
4. Choose **Start recording** unless automatic recording is already enabled, add
   numbered **Marker** entries as needed, and choose **Stop recording**. Sensors
   continue until **Stop sensors** or **Exit**.

Automatic naming is enabled by default. Disabling it asks for a session-name
prefix before recording; it does not change the existing recorder's collision-safe
timestamp/session naming. Optional automatic recording starts when sensors are
explicitly started, not on application launch.

Keyboard alternatives: Ctrl+Shift+S starts sensors only when they are stopped,
Ctrl+Shift+X stops them only when they are running, Ctrl+R toggles recording only
when that button is enabled, Ctrl+M adds a marker, and Ctrl+K opens Fusion setup.
**Start recording** is disabled while automatic recording is enabled and a recording
is not already running. Unavailable toolbar actions are also unavailable through
these shortcuts.

The default has no measured mount: horizon and vehicle acceleration are deliberately
unavailable. A down-only capture cannot establish the full installation and is not an
alternative acceptance path. Even with a profile, gravity requires a supported quiet
stop, fresh original zero-speed observations and valid gyro coverage. Green acquisition
status does not establish these conditions. **Diagnostics** and the setup window show
support independently for attitude, acceleration, speed, course, body heading, position
and road geometry. Mount provenance travels in the engine configuration used by recording.
Support remains conditional, not a claim of validated road accuracy; body attitude is
not road grade/bank. See `docs\testing\Vehicle-Fusion-Road-Capture.md` for a pilot capture.

**Special / Merc sensors** replaces only the right instrument column with a compact
2-by-2 group: engine speed, coolant, fuel/battery state of charge, and
**Throttle (act/comm'd)**. Actual and commanded throttle have independent percentage
rails; the pedal-position CAN candidate is not substituted for either.
The gauges occupy one third of the column height, with **CAN Diagnostics** using
the remaining two thirds. Readouts and rails scale together as a centered group.
Speedometer tiles use 4 km/h spacing: five tiles per 20 km/h on both rings.
The speedometer also shows forward-acceleration/braking perspective LEDs **behind**
its scale, speed LEDs and readouts, using the acceleration instrument's geometry
and green/red response. FusionGauges enables automatic mode with four distinct
speed measurements. The estimate is a least-squares speed slope using measurement
timestamps (not repaint timing), converted to g; quantized/delayed speed can still
produce noise. Invalid speed, source changes, backward time or gaps over three
seconds restart the window; repeated UI presentations do not add measurements.
The reusable `D2DSpeedometer` exposes `AccelerationMode` (`Off`, `Auto`, `Manual`),
`AccelerationSampleCount` (minimum 3, default 4, maximum 10000) and
`ForwardAccelerationG` (positive acceleration, negative braking, NaN unavailable).
Set the latter to feed Manual mode; Auto ignores that manual input. Off is the
library default and hides even the unlit panels.
The horizon and compass are hidden, not disposed, and return when Mercedes mode is
turned off. The default three-column layout is otherwise unchanged. RPM is stale
after 3 seconds, as is throttle; coolant, fuel and battery after 5 seconds.
Missing telemetry stays unavailable independently on each rail.
CAN lines come from `SensorHub.CanDiagnosticReceived`.

Enabling **Merc sensors** queues at most **three investigative CAN attempts** within
a 30-second capture budget, then restores ordinary OBD polling. Investigative
capture does not restart on reconnect or repeated enable calls; toggle the option
off/on explicitly to run another batch. Turning it off cancels an active batch.
**Double-click CAN Diagnostics** to open a selectable command/signal results
snapshot: attempts, received frames, adapter responses, raw signal values, decoded
candidate values/units and timestamps. This list survives the terminal's 400-line
trim and does not initiate polling. Adapter `OK` is not vehicle data; absent frames
are not evidence of unsupported signals, and Mercedes CAN decodes remain unverified.

**Clipboard** offers **Copy Diagnostics**, **Copy Log**, **Copy CAN Diagnostics**
and **Copy all**. These copy the full current text, including hidden panels, not
only the selection. Copy all separates the three panels with headings. Clipboard
content may contain location and recording paths; review it before sharing.
See [Mercedes capture conclusions and adapter research](Mercedes-CAN-Analysis.md)
for the supplied capture findings and read-only acquisition options.

**Special / Reset display** clears the instrument snapshot and marker counter. It
does not stop acquisition or recording.

**Exit** keeps the shell enabled and shows a selectable 12-point Cascadia shutdown
log (Consolas if Cascadia is not installed). Warning and error lines are colored.
The same lines are written to `ILogger<MainForm>` when the host registered one.
Commands other than the final Exit acknowledgement are disabled. A clean shutdown
leaves the selectable log visible for about four seconds, then closes. Warnings or
errors stay on screen until Exit is chosen again. The log is not replaced by a task dialog.

### Landscape motion-axis calibration

With sensors running and recording stopped, choose **Tools / Landscape motion
calibration** while parked. Hold the whole tablet in landscape and capture these
three motions separately: **pitch** (top edge toward you first, then away),
**roll** (right edge down first, then left edge down), and **yaw** (turn right
first, then left, keeping the tablet upright). Each step has a three-second
countdown followed by six seconds of back-and-forth motion. Return to the same
starting posture between steps. Move the tablet itself, not just its stand.

The wizard consumes an independent full-rate raw IMU subscription, not UI frames.
It measures a signed direction in raw gyroscope XYZ for each motion, checks
single-axis purity, motion in both directions, sample continuity, and independent
near-perpendicular axes. Insufficient motion, unavailable gyro data, mixed axes,
and dropped samples reject the capture; repeat a step or start over. Cancel
releases only this wizard's subscription and leaves acquisition running.

**Keep diagnostic mapping** adds the measured directions and live mapped angular
rates to Diagnostics for the current sensor session. Signs mean the instructed
first stroke, not an assumed Windows pitch/roll/yaw convention. The wizard is an
axis-identification aid: it does **not** silently replace fused attitude, modify
raw recordings, establish absolute yaw, or solve the separate vehicle-forward
mounting ambiguity of gravity-only calibration. A new sensor session clears the
mapping. It does not replace the measured physical installation in **Fusion setup**.

F11 or **Full screen** toggles .NET 11 `KioskModeManager`; Escape returns to the
windowed view. Toolbar and status remain visible, while the menu hides.
Display/system stay awake while the form owns the kiosk manager. Closing releases
the request and saves restored window bounds rather than full-screen bounds.

### Options

| Page | Settings |
|---|---|
| Appearance | Small/Medium/Large icons: 24/36/48 logical pixels; fonts: 9/10/12 pt. Defaults: 36 px and 10 pt. Acceleration precision: 0–5 decimals (default 2). Each speedometer ring has a saved color, caption and logical caption padding. Icons scale with monitor DPI. |
| Sources / GPS | Windows IMU/Location, IMU-only, GPS-only, health-only, or IMU plus serial NMEA; reporting interval, COM port/automatic discovery, GNSS profile, and receiver-command authorization. |
| Recording | Local base folder, mandatory V2 pair and compact journal, optional verbose JSON, raw snapshot rate, filename prefix, automatic naming/recording, and marker label. |
| ZIP / OneDrive | Optional locally synchronized destination folder and compression level. Disabled by default. |

GPS changes apply immediately, including during recording, without restarting IMU,
the drive engine or recording. IMU-mode changes apply at the next drive Start.
Calibration stays available during recording and while stopped; Options and GPS selection remain available.
Receiver-command authorization is session-only and
is never serialized; non-passive receiver profiles still require explicit consent.
COM-port discovery does not open a receiver. An open serial transport is not proof
of a valid GPS fix: the cockpit separately checks accepted-fix freshness.
GPS preview continues after **Stop sensors**, without running drive inference or
classification. Missing serial GPS retries 15 seconds after each failed attempt
during the first recovery minute, then every 30 seconds. New COM selection cancels
probing/waiting, awaits safe release, then opens only the latest selected receiver.
Unverified release/restoration blocks further opens and is visible as a GPS fault.
Automatic recovery remains read-only; new receiver-writing changes require a fresh
stationary fix, or the explicit parked-vehicle confirmation at Start.

### Speed source: OBD2 or GPS

The **Speed source** toolbar command has **OBD2** and **GPS** entries. GPS is
the default. Its existing Windows/Serial, GPS Type, COM Port and Target Res.
menus live below **GPS**; **Use GPS** returns to the original GPS/fusion display.
GPS receiver changes and speed-source selection take effect immediately and are saved.
The canonical recording stream captures source handoffs, generations and OBD policy
changes, rather than silently restarting a recording.

At startup, discovery and connection run on a worker, using the existing WARP
`ObdLinkConnection` from `WarpToolkit.Windows.Bluetooth`, also used by Sensored.
Discovery/reconnect has a ten-minute search window, with at most 20 seconds per
discovery/connect attempt. Established connections survive the search deadline.
The adapter choice is remembered after quitting. Changes apply after restarting the app,
without interrupting the current connection or recording. AD20 uses standard ELM commands,
not the OBDLink-specific `STDI` identity command. Its firmware must expose a supported
diagnostic BLE UART and accept ELM commands; unsupported endpoints or identities are
reported as connection errors rather than silently selecting another device.
Only the selected already-paired adapter is used; there is no
automatic pairing, persistent adapter configuration or vehicle-changing command.
If unavailable, check Windows pairing, power/range, ignition/ECU availability and
other apps holding the adapter, then restart the app. Closing cancels and awaits
discovery/acquisition before releasing its transport.

With **OBD2** selected, vehicle-reported Mode 01 PID 0D speed is the sole speed
input. The speedometer shows km/h and mph simultaneously, without damping,
GPS comparisons or inertial/GPS blending. Its trip comes from full-rate OBD
measurements, not presentation ticks. Missing/error/disconnected data or a reading
older than 300 ms displays unavailable; it never becomes a zero or a GPS fallback.
Zero is accepted only when actually returned by the ECU.

The same authoritative magnitude constrains the fusion engine's horizontal
velocity and position propagation. GPS can still contribute position/course and
IMU can contribute attitude/direction; neither changes OBD speed. Saved historical
steps retain their own speed constraints for delayed GPS correction. Speed has
**100% confidence while fresh**, independently of the existing attitude/position
confidence. This is the requested source-selection policy, not a claim of
certified accuracy: standard PID 0D has 1 km/h resolution and host reception
timestamps, not a verified ECU measurement clock or guaranteed dashboard value.

The dedicated speed-only session targets **20 Hz**, filters one advertised ECU,
requests one response and never interleaves slower telemetry PIDs. The status bar
reports measured valid-reply Hz and command round-trip milliseconds, and warns
below **10 Hz** after an initial observation window. Hardware/radio/ECU delays
cannot be guaranteed away; a 250 ms command timeout ends a desynchronized session.
GPS behavior and its presentation cadence remain unchanged when GPS is selected.

### CarMode and GPS shortcuts

Both samples enable the shared `SymbolToolStrip.CarMode`. Parent navigation occurs
only on completed activation, never on mouse-down or submenu hover. This
prevents releasing a parent click from accidentally selecting a command in the new row.
Clicking a parent replaces
the toolbar row, rather than opening a popup: Back, separator, enlarged semibold/bold
title, and the original submenu commands appear horizontally. The background becomes
20% lighter in dark mode or darker in classic mode. A trailing separator and SVG
**Go to Root** button cancel navigation. Escape/Alt+Left go back; Ctrl+Home returns to root
when the toolbar has focus. Commands retain their original instances, handlers,
checked/enabled state, and dropdown layout when restored. Four effective levels
(including the root row; hidden branches excluded) are supported; enabling a deeper
tree throws with the offending item's path. Leaf commands do not automatically
navigate; these GPS setting commands deliberately return to root after saving.
FusionGauges uses the same submenu return delays as Sensored: 5 seconds after a
leaf command and 15 seconds while browsing. Submenu arrows are drawn beside the
icon. The reserved right padding remains so the symbol is not covered.

**GPS-Configuration** offers Windows / Serial, actual named GNSS profiles under
**GPS Type**, COM1–COM9 with the saved port first and not duplicated, and
**Target Res.** with the saved target first, then 1000/500/200/100 ms and Dynamic.
The saved port is included even when outside COM1–COM9. SVG icons scale with the
toolbar. Configuration applies through the shared GPS owner without silently
restarting acquisition or recording. Choosing an active serial profile
requires explicit, non-persisted receiver-command consent.
Both samples use WARP's `GpsReceiverCommandConsent` prompt. Selecting a target from
Passive chooses **Current** (rate only): the library preserves CFG-GNSS and changes
only the volatile fix interval. The Sources / GPS options interval also applies to serial receivers.

For Windows, Dynamic requests 100 ms; the provider decides the actual rate.
For serial, Passive never changes receiver settings. An authorized, supported
receiver receives the exact selected volatile interval, or Dynamic negotiates
100/200/250/500/1000 ms in order with ACK/readback verification. An exact rejected
target reports failure and restores the original configuration, rather than
silently substituting a slower target. Neither mode guarantees the rate of valid fixes.
See [COM8 frequency measurements](GPS-Frequency-Tests.md) for observed rates and
verification through both samples' full-rate processing owners.

## Processing and histories

Acquisition and service-owned raw recording do not depend on rendering. The hub
uses independent unthrottled All subscriptions: raw observation capacity 4096 and
sequential fusion capacity 8192. A UI timer presents latest immutable snapshots at
nominal 30 Hz using elapsed-time gating; this is not a real-time scheduling guarantee.
No per-sample UI dispatch is performed.

`SensorObservationCore` retains 120-second arrival-indexed IMU, GPS, and fusion
histories. Inclusive range reads allocate only returned values. Regressing arrival
indices are clamped without rewriting source timestamps. Rates use a two-second
arrival window; fewer than two samples yield 0 Hz. IMU/fusion become stale after
one second and accepted GPS after three seconds. Subscription drops and ingress
gaps are reported separately.

**Stop sensors** pauses sensor presentation immediately and shows **Stopping**
while recording finalization and source cleanup finish. The app then displays a
frozen final snapshot with zero arrival rates, historical-reading labels, and
fixed history counts; it does not keep counting retained samples down to zero.
Starting a new session clears that snapshot and resumes live presentation.
The UI timer is off while idle. Pending ZIP work is polled at 1 Hz using the
frozen sensor snapshot, so recording/mirror progress and failures remain visible
without aging sensor history. Options and retry commands also refresh the display
once without restarting live sensor polling while stopped.

This stop-display policy belongs to FusionGauges, not the shared sensor library.
`SensorObservationCore.Capture(now)` still advances rolling retention for other
callers. The change does not skip data finalization or shorten hardware teardown.

`SensorFusionSession.Processed` observes every input on its sequential owner.
Handlers must not block or write files. `ReplaceEngineAsync` resets/replaces or
disables the engine between inputs and increments `Epoch`; golden file-backed
reference sessions intentionally reject replacement. `AdvanceFusionEpoch` clears
old fusion presentation/history without discarding raw histories. There is no
engine-selection UI in this iteration.

Gauges use validity flags and `VehicleGaugeReadings`, not raw acceleration as a
fallback. Vehicle axes are forward/right/down, attitude is degrees, fusion
acceleration is m/s2, and the projection converts it once to g. Primary speed uses
valid fusion navigation; GPS receiver speed remains independent. Unknown, invalid, and stale values reset presentation
instead of becoming plausible-looking zero readings.

### Shared gauges and telemetry

Both FusionGauges and Sensored use the gauge design and channel policy in
`WarpToolkit.WinForms.Sensors.DirectX`, including Sensored's three-source compass.
Magnetic heading, fusion yaw (or explicitly labeled device yaw), and GPS
course/track have independent validity and legends.

The speedometer retains its nominal 0–160 km/h / 0–100 mph scale, central speed,
GPS comparison, trip and altitude history. Scale numerals have received a further
2 pt increase (4 pt above the trip font); the trip drum is inside the instrument panel.
`InstrumentNumeralPositioning` selects `OuterPositioning`, `InnerPositioning`, or
`Invisible`. Both apps default to bordering the innermost LED circle on its inside,
with a saved numeral margin of 5 logical pixels, scaled with DPI.
Preferred sizing reserves room for the captions and receiver comparison at the current font/DPI.
Two radial LED rings replace the needle and ticks: outer for the main reading,
inner for the secondary readout. GPS receiver comparison uses the saved blue profile;
comparison in the other unit uses the saved red profile. Default captions follow the
units/source; custom captions and all existing saved appearance profiles are preserved.
Outer tiles are 20% shorter radially (1.6:1 rather than 2:1). Secondary tiles retain
their previous radial length and common tangential height.
The packed rings have 1-logical-pixel radial gaps and a 3-logical-pixel outer
corner inset, scaled with DPI. Tile height fits the tightest inner-ring spacing
without overlap, leaving a larger inner boundary for the numerals.
Below a 240-logical-pixel instrument radius, the outer tiles taper progressively
toward the center (up to 12% narrower at the inner edge at radii of 180 or less).
Larger instruments keep rectangular outer tiles; the threshold is DPI-independent.
Every ring uses the same 33 five-unit angular positions and common numerals.
The inner ring plots the secondary numeric reading against the selected scale.
Equal readings in the same unit illuminate equal angles. Removing the third ring and
shortening the outer tiles gives the numerals a larger radius inside the remaining inner ring.
Each five-unit tile
has five lit levels plus off; completed tiles remain lit. Handoffs overlap:
4 → `[4,1]`, 5 → `[5,1]`, 9 → `[5,4,1]`, 10 → `[5,5,1]`.
LEDs floor fractional input, but readouts retain their precision.
Double-click changes the numeric scale/readout/trip units; the two rings stay tied to
the main and secondary readouts.
Each caption character is individually positioned and rotated along its ring.
Ring captions allow at most 15 characters, and default to
5 logical pixels of caption-to-first-tile padding. Colors are white/green/red/blue.

The center groups are **SPEED**, **GPS**, **ACCELERATION (Sensor)** and
**INCLINOMETER (Sensor)**. Numeric labels use the form's base point size, preferring
CaskaydiaCove Nerd Font, Cascadia Code, then a system monospace fallback.
Speed/trip layout ranges are 0–300 and 0–9999; real values are not clamped.
GPS coordinates use seven decimals, speeds one, and other readings two, in
the current culture. GPS heading is receiver course, accuracy is horizontal
accuracy in meters, and Source identifies the provider.

Calculated GPS speed uses a four-fix path-distance/time window. Full-rate
accepted GPS fixes drive trip and altitude independently of UI cadence.
Trip resets only for a new acquisition session; gaps/source changes break
continuity without bridging distance or clearing accumulated trip.
Raw acceleration is device-frame **g including gravity**, not fused acceleration;
raw pitch/roll/yaw are degrees. **Yaw Accuracy: Unavailable** is intentional:
the sensor payload supplies no yaw-accuracy field. Stale/missing readings
remain unavailable rather than borrowing values from another channel.
The acceleration instrument's perspective LED panels are a generic library feature:
`D2DAccelerationGauge.ShowLongitudinalLedPanels` defaults to true and can be disabled
independently of its readings, ball, shock visualization and damping.

## Recording compatibility and companions

All three existing raw formats are enabled by default: verbose JSONL, compact
JSON, and semicolon CSV. Raw schema version 3, timestamp encoding, CSV fields,
part rotation, classification sidecars, and `manifest.json` are unchanged. Existing
uncompressed readers and recordings remain compatible with Sensored and
WarpSensorsDemo. Fusion and markers are **not** inserted into raw rows.

`SensorRecordingCompanions` adds these session-local artifacts:

| Artifact | Meaning |
|---|---|
| `fusion-epoch-NNNN.csv` | Existing `FusionTraceWriter` format, one file per engine epoch. |
| `fusion-epoch-NNNN.jsonl` | `warp-fusion-observation`, version 1: recording ID, engine/epoch, raw input sequence, ISO source/arrival timestamps, units, and the immutable estimate including validity. One row per non-null estimate. |
| `markers.jsonl` | `warp-sensor-marker`, version 1: recording ID, ISO timestamp, label, input-sequence anchor, and epoch. |
| `companions.manifest.json` | Complete flag, raw sequence coverage, first/last companion input, input count, epoch/engine identities (including disabled epochs), rejected-entry count, finalized filenames, and failure evidence. |

Use an All-kinds raw recording, publish every fusion input in order, stop raw
recording, drain fusion through its last accepted sequence, then call
`StopAsync(inputsVerified: true)`. Full coverage requires matching boundaries,
unique ordered inputs, matching raw row count, and no raw/companion loss or fault.
Unknown coverage is incomplete, never assumed complete. Marker queue overflow
throws; fusion rejection faults companions. The app backfills the recording start
boundary from a bounded recent-input cache and verifies it at stop.

Files remain `.writing` until recording finalization, including closed earlier
epochs. Unverified outputs become `.incomplete` and stopping reports failure.
Treat companions as complete only when their successfully published manifest
explicitly says `Complete: true`; interrupted or failed finalization can retain
working artifacts for diagnosis.

## Finalized-part ZIP mirroring

`SensorRecordingZipMirror` queues closed files with `QueueArchiveAsync`, exposes
`Status`, and supports `RetryAsync` and durable restart via
`zip-mirror.journal.json` in the local session folder.

The app queues each finalized raw part with its sidecar and queues companions and
manifests at recording stop, including `recording.manifest.json` and the causal
replay inputs, controls and manifest. A single process-wide compression gate and 64 KiB I/O
buffers keep compression independent of acquisition. Incomplete ZIPs are staged
locally, closed, copied through a temporary destination, then renamed to a final
ZIP. Existing unrelated archives are never overwritten; idempotent publication
checks identity, SHA256 metadata, and decompressed payload bytes.

Mirror errors do not stop local recording. The Recording page/status strip retain
failure and backlog information. Three automatic attempts are followed by manual
**Retry ZIPs**, which also scans local recording journals from previous runs.
Exit cancels current mirror work and preserves its journal instead of waiting
indefinitely for a cloud connection.

**Published means copied to the configured sync folder, not uploaded to OneDrive.**
Authentication and remote synchronization remain the sync client's responsibility.
Keep the local recording folder separate from the synchronized destination.

`SensorRecordingReader.ReadArchivePartAsync(archivePath, entryName)` reads a safe,
explicit raw entry through the unchanged format parser, without extracting files.
ZIP is only an outer container; ordinary `ReadPartAsync`/`ReadFolderAsync` still work.
ZIP mirroring does not automatically delete local originals.

GPS-only sessions (or an IMU that has not started reporting) must also finalize
cleanly. Pending receiver/pattern support is a bounded derived cache, not a
lossless recording queue; old derived support is trimmed with explicit unavailable
history/continuity diagnostics rather than stopping the fusion consumer.
The raw journals and causal replay history still retain every accepted input.
Journal-backed CSV context evictions are counted in `CsvContextTruncations` in
the companion/V2 manifests and annotated in the next CSV row. These are not
dropped recording inputs; standalone golden traces still fail on provenance
overflow rather than silently relaxing their contract.
An existing incomplete recording cannot be certified complete by retrying its
ZIPs: retry republishes preserved artifacts, it does not reconstruct missing
processed inputs. Original local recordings are left untouched.

## Deliberate work-order deviations and deferred work

The confirmed implementation uses .NET 11, separate full-rate raw/fusion
subscriptions, all three existing raw formats, and fusion CSV/JSONL plus marker
companions. ZIP compression begins **after part finalization**, not as a concurrent
live compressing stream. These choices preserve raw format compatibility and
isolate recording, fusion, and cloud-folder failures.

Feature migrations into Sensored and applicable WarpSensorsDemo surfaces are
deferred until these shared APIs land on main. The follow-up branch must start
from freshly fetched main; no prerequisite cherry-picks are part of this iteration.
Compilation regressions in those samples are checked independently of that migration.

## Deterministic verification

```powershell
dotnet test src\FusionGauges\FusionGauges.Tests\FusionGauges.Tests.csproj -m:1
dotnet test src\Tests.UnitTests\cs\WarpToolkit.Sensors.Tests\WarpToolkit.Sensors.Tests.csproj -f net11.0 -m:1
dotnet test src\Tests.UnitTests\cs\WarpToolkit.Sensors.Fusion.Tests\WarpToolkit.Sensors.Fusion.Tests.csproj -f net11.0 -m:1
```

Tests use injected sources/streams, local file I/O, and real Designer-created
controls; they do not require physical sensors or a OneDrive account. Coverage
includes retention/rates/loss, engine epochs/end-of-input commands, all raw formats,
recording boundaries/shutdown, incomplete companions/writer failures, ZIP byte
compatibility/retries/multipart output, settings consent, fonts/icons, and full-screen
restoration. App tests are also registered in `src\Warp.slnx` for CI.
## Road recordings

The drive keeps a temporary disk-backed causal input/control journal from fusion
consumer start, independently of recording. A finalized recording also exports
`replay-inputs.compact.json`, `replay-controls.jsonl` and
`replay-inputs.manifest.json` through its exact accepted cutoff. These include
pre-recording warm-up and the speed selection/value, tablet capture/lock state,
engine identity and epoch consumed at each owner boundary. They are included in
the finalized companion ZIP. The temporary journal is removed on drive cleanup;
overflow/export faults prevent a complete recording claim.

Speed updates are applied on the fusion owner immediately before processing,
not concurrently between a recorded control snapshot and the corresponding
engine call. This does not filter or blend the selected source. These companions
support deterministic offline replay; merely opening the compact input file
without its control sidecar does not restore its speed/mount policy.

Use the [WARP road-capture protocol](https://github.com/KlausLoeffelmannOrg/WARP/blob/main/docs/testing/Vehicle-Fusion-Road-Capture.md)
for route selection, safe maneuver pairs, searchable markers/notes and evidence
limits. Configure while parked; only a passenger may operate recording controls
or add markers while the vehicle is moving.
