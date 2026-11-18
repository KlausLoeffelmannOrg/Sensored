# Sensored

`Sensored` is the driving-support WinForms client composed from `WarpToolkit.Sensors`,
`WarpToolkit.Windows.Sensors`, `WarpToolkit.Sensors.Presentation`, and
`WarpToolkit.WinForms.Sensors.DirectX`. It demonstrates service-owned sensor execution, independent
async streams, raw recording, replay-compatible files, road-section classification, and a separate
portable causal-pattern vehicle fusion engine with explicit per-quantity support.

## Application split and independent user data

New recordings always contain a matched `Filename.RawV2.csv` / `Filename.FuseV2.csv`
pair and a mandatory full-rate compact-JSON replay journal. Verbose JSON is optional;
new recordings no longer write the legacy envelope CSV, whose reader remains supported.
Replay accepts `.compact.json`, `.jsonl` and legacy journal `.csv` parts; select a
journal, not `RawV2`/`FuseV2`. Navigation uses the same full canonical arrival
timeline as playback and OBD warm-up. Historical health/classifications are replayed
as recorded rather than regenerated as current hardware observations.
In **Options / Recording**, choose 1..20 raw snapshots per second (default 10,
applied at the next recording). Fusion output remains full-rate and separately logs
unblended GPS/OBD speeds. Unsupported OBD quantities are blank with explicit status.
Trust the terminal `v2.manifest.json` coverage flag, not the existence of a CSV alone.

The recording viewer opens finalized `RawV2` and `FuseV2` CSVs directly, or selects
between them when opening a recording folder. It shows the recorded fusion outputs;
it does not rerun fusion for V2 files. Acceleration, gyroscope, magnetometer and
attitude triples share multiline columns. Right-click a column to choose 0..9
decimal places (default 3). **Average (ms)** selects 50..1000 ms buckets in 50 ms
steps. Each channel averages only its finite, supported/valid samples; empty
buckets are not fabricated. Direction angles use circular means across north;
source/generation and engine-epoch changes break buckets rather than blend
unrelated inputs. Hover raw cells for valid-sample counts and the latest
channel provenance. **Raw selected row** retains the first original CSV row in the
bucket at full precision, including all state and provenance fields. V2 browsing
does not enable the legacy recording editor.

The fuel/battery instrument displays the fresh OBD PID 5B battery percentage as
well as fuel level; disconnected, invalid or stale battery values stay unavailable.
The taller RPM instrument also displays independently fresh commanded (`T-com:`)
and actual (`T-act:`) throttle percentages. Mixed OBD acquisition polls every
supported metric, including battery state of charge PID 5B; voltage is never
substituted for remaining charge. Unsupported measurements remain unavailable.
RPM, temperature and fuel/battery share one rendering host beneath the speedometer;
acceleration sits beneath the horizon. Measurement definitions live in their actual
gauge controls, not in the common renderer or duplicate sample controls.

Single-click/tap the speedometer or horizon to show its current source mode.
Double-click/tap to cycle modes. The full caption holds for three seconds, fades
for one second, and restores the ordinary indication; the mode letter stays visible.
The horizon has seven alphabetic attitude/heading-source combinations.
Speedometer modes are saved:

| Mode | Primary | Secondary |
|---|---|---|
| A | km/h (OBD2) | mph (OBD2) |
| B | km/h (OBD2) | km/h (GPS) |
| C | km/h (GPS) | mph (GPS) |
| D | mph (OBD2) | km/h (OBD2) |
| E | mph (OBD2) | mph (GPS) |
| F | mph (GPS) | km/h (GPS) |

OBD modes explicitly indicate GPS fallback when OBD speed is unavailable. In that
case the secondary readout uses the other unit instead of comparing GPS with itself.
Instrument mode selection never changes the fusion input policy.

### Automatic acceleration intervals

Live acquisition passively detects standing 0–100 km/h or 0–60 mph runs, according
to the primary unit, and a parallel quarter mile (402.336 m). A standing start
requires an observed speed at or below 1 km/h and acceleration beginning below
5 km/h. Considerable acceleration is at least 0.5 m/s². Rolling 80–120 km/h runs
arm in the 77–79 km/h band only when acceleration began within the preceding
second and remains at least 1 m/s². This rolling interval includes the requested
early arming allowance; it is not a certified interpolated 80-km/h start.

GPS observations explicitly reporting no fix never arm timing.
Detection uses original OBD/GPS timestamps and optionally supported, gravity-compensated
forward acceleration, never raw gravity or UI damping. Detection adds the requested
initial 0.3 seconds by backdating the start. After one second of sustained acceleration,
the face turns dark red, the primary speed becomes yellow and two points larger, and
the secondary readout becomes a stopwatch quantized to 50 ms.
0–100 finishes at 99 km/h for OBD or 98 km/h for GPS; 0–60 finishes at 60 mph;
rolling runs finish at 120 km/h. Threshold and quarter-mile crossings are interpolated
between original measurements. The speed milestone stops the visible watch and holds
the result briefly; quarter-mile integration continues independently while accelerating.

Braking or loss of considerable acceleration cancels unfinished intervals.
Speed-based detection uses a 0.4-second window to tolerate integer OBD quantization;
supported acceleration detects a lift immediately. Source/unit changes, backward
time, gaps exceeding 1.5 seconds, stale input, pause and session resets also cancel.
Replay never creates live results. Completed results survive subsequent resets and
are appended once to `acceleration-runs.jsonl` in the configured recording folder,
with run ID, kind, source, UTC start/finish, elapsed time and a causal GPS position
no more than two seconds old, or explicit null coordinates when unavailable.
Storage errors are reported and leave results pending rather than claiming success.
These are approximate passive measurements, not certified performance figures.
Use performance measurements only where lawful and safe; do not operate the UI while driving.

`D2DSpeedometer.ForwardAccelerationG` is bindable, with change notifications for
valid values and transitions to unavailable. Auto mode derives it from distinct,
timestamped, undamped primary-speed measurements, not GPS comparison values or
render timing. Switching sources restarts the acceleration window. This derivative
has optional instrument-owned damping in **Options / Source** (the first page):
**None** preserves the current four-sample estimate; at 10 Hz **Light**, **Medium**
and **Strong** use five, seven and nine measurements. Slower sources reduce the
window in proportion to their observed cadence, with at least three measurements.
Repeated UI frames do not count as new measurements. Damping affects the bindable
acceleration and its LEDs, not speed, distance, recordings or fusion inputs. This derivative
can constrain forward-axis calibration: it has no additive gravity term, but it
is tangential acceleration along the vehicle path, not a complete body-frame
acceleration vector or an incline measurement. Slip, GPS noise, quantization,
turning and timestamp uncertainty still require quality gates. The fusion engine
already receives source-speed observations at full rate; calibration must use
those causal observations, not feed the UI-throttled property back into fusion.
It still requires independently supported mounting/gravity evidence.

**Ctrl+M** adds a numbered timestamped marker to the current recording. Set its
note/label in **Options / Recording** while parked (or have a passenger handle notes).

See the [WARP road-capture protocol](https://github.com/KlausLoeffelmannOrg/WARP/blob/main/docs/testing/Vehicle-Fusion-Road-Capture.md)
for route types, safe maneuver pairs, marker/notes conventions and reference limits.
The marker retains canonical input sequence/engine epoch for later segment lookup.

### Opt-in road learning Quizz

**Start learning Quizz** replaces **Driving tasks** and **Fusion feedback** on the toolbar.
Start live acquisition first, then explicitly start a learning session when safe.
Only a passenger may interact while moving. Calibration in Tools/Options and the
**Fusion setup** toolbar command are unchanged.

The modeless, large-text window collects a rolling 30-second context. A different
condition must remain the candidate for two seconds before it is asked; a changed
level alone does not repeat the last asked condition. Tap **Level 1..5** to confirm
the named condition with your subjective severity, or **0 - Wrong assessment** to
reject only that condition. There is no default answer. Questions expire after ten
seconds; silence, dismissal and pauses never produce labels.

**Disregard Last** durably retracts the last non-retracted answer in this session.
The arrows select **1..20 minutes** (initially five); **Pause** dismisses the
question and suppresses prompts while sensing continues. **End Quizz**, closing
the window, stopping/restarting acquisition, entering replay or shutdown ends the
session. Pending writes finish before the window closes.

The StatusStrip exposes GPS speed/acceleration, vehicle speed/acceleration/RPM,
internal accelerometer/gyroscope and Bluetooth accelerometer/gyroscope evidence.
Tap any field for a larger table; this discards the question before opening the
table and cannot create an answer. Missing/stale sources contribute nothing.
Reported percentages are **experimental rule scores/evidence shares, not
calibrated probabilities or independent gains in accuracy**. RPM, gyro and
supported body pitch are retained as context, not assumed to establish a class.

The cold-start interpreter (`road-rules-v1`) covers the whole catalogue with
explainable motion rules over the 30-second window:

| Condition(s) | Evidence used | Level |
|---|---|---|
| Rough / wavy road | IMU magnitude variability; 1 s means separate waves | vibration |
| Acceleration / braking | source-time speed slope (vehicle, else GPS) | m/s² |
| Emergency braking | speed drop > 5.5 m/s² in the last 4 s | none |
| Level→uphill/downhill, downhill↔uphill | supported pitch, first 10 s vs last 5 s | grade (level→hill only) |
| Up/downhill + left/right curve | supported lateral acceleration + pitch | driving style |
| Starting off uphill / level start | start from standstill + pitch | grade / acceleration |
| Stopping uphill / braking to standstill up/downhill / level | stop + pitch; gentle (< 1.5 m/s²) vs braking | grade / deceleration |
| Waiting at traffic light | stopped ≥ 12 s after moving | none |
| Waiting at stop sign | full stop 0.5–6 s, then straight departure | none |
| Intersection stop | short stop, then ≥ 45° GPS course change or strong lateral | none |
| Traffic jam stop and go | ≥ 2 stops in the window below 12 m/s | none |
| Highway driving | ≥ 20 s above 20 m/s, steady speed | none |
| Enter / exit highway | crossing 17↔22 m/s with ramp evidence (course ≥ 45° or lateral ≥ 1.5 m/s²) | none |
| Driving backwards | longitudinal acceleration opposing speed slope, or GPS course flip across a stop | none |
| Parallel parking | < 3 m/s, ≥ 2 stops, a reverse segment | none |

Without pitch the start/stop rules fall back to "grade unknown" classes; a plain
stop without prior motion is "Stopped". Semantic causes (light, sign, ramp,
parking, jam, reverse) are inferred from motion only and start at a lower score.
These are unvalidated proxies: body pitch is assumed nose-up positive and only
approximates grade; a stop-sign/light split by stop duration and a ramp by turn
plus speed change can be wrong, which is exactly what the passenger's answer
records. Conflicting GPS/vehicle speeds cause abstention. Levels are provisional
thresholds the passenger can correct.
Each answer is a self-contained, atomically replaced JSON example under
`%LocalAppData%\WARP\Sensored\RoadLearning\quiz-v1\<session-id>\<question-id>.json`.
It includes schema/model versions, the frozen guess and score, the mount/source
profile, evidence table, optional recording path, and up to 20 Hz per-source
samples from the last 30 seconds with original timestamps, sequence, epoch and
generation. Undo preserves the original example with `Retracted: true`.
Level zero is a rejected guess, not a corrected class or a negative for other
classes. Storage errors are reported, not counted as saved answers.
Gaps, source/generation/epoch changes and clock discontinuities require a fresh
window. Replay does not collect quiz examples. Historical `driving-task-v1`
recording markers and `FusionLearning\evidence-v1.json` remain compatible; legacy
evidence is retained separately and is not imported as training truth.

**Architecture discussion (Fable 2, Grok 4.7 and GPT Astra 6.2):** all favor a
separate semantic interpretation layer over changing physical fusion from user
labels. Fable favors per-condition logistic/perceptron units with source
interaction features; Astra and Grok recommend collecting versioned examples and
keeping an explainable baseline before fitting weights. The agreed rollout is
offline training, excluding retractions and treating missingness explicitly,
then evaluation on whole held-out drives/profiles and shadow scoring before
promotion. Adjacent overlapping windows must not straddle train/test splits.
Compare false prompts at matched coverage and check probability calibration
before describing a score as agreement probability.

There is **no online training or learned model yet**. Quiz labels never change
gravity, sensor weights, mounting or physical support. Even a future restrictive
fusion veto needs independent replay validation; labels alone cannot justify it.

The root `Sensored.slnx` uses WARP NuGets; `Sensored.Dev.slnx` uses sibling WARP projects.
The executable project is `src\Sensored\Sensored\Sensored.csproj`.
`WarpSensorsDataProcessor` and its tests retain their names and move with `FusionReference`.
Real recordings remain private in WARP; an optional local `ReferenceData` copy is Git-ignored.

The private WARP `samples\cs\Sensored\ReferenceData` includes all 25 raw recording parts as semicolon CSV files, plus the
classification and session-metadata JSON companions. The CSV files contain the complete
sensor rows and timestamps needed for replay; duplicate `.compact.json` and `.jsonl`
recordings are omitted to keep the demo data smaller.

Raw-sensor diagnostics and GPS Playground are now part of the separate
`samples\cs\WarpSensorsDemo` diagnostic app in the WARP repository. Sensored no longer opens those dialogs or
their GPS map tool window. Its driving map, local help, source options, recording timeline,
recordings, and replay remain available.

Sensored starts with independent settings under `%AppData%\Sensored\<assembly-version>`
and a separate package identity (`KlausLoeffelmannOrg.Sensored`). Logs, new recordings,
and fusion traces default to `%LocalAppData%\WARP\Sensored`. No old settings, recordings,
logs, exports, or other user files are migrated, moved, or deleted. Existing recordings
can still be opened explicitly for replay or in the recording timeline; Options can select
a recording folder.

## What the application does

### Attitude, direction and independent sensor references

The spherical horizon is enabled by default in Sensored; **Tools / Options / Display**
can restore the traditional horizon. The library control itself remains opt-in.
In spherical mode, OBD instruments use the former compass slot and the map regains
the full map-panel width. Double-click the horizon to cycle:

| Mode | Pitch / roll | Direction |
|---|---|---|
| 1 (default) | Internal-IMU fusion | GPS course, otherwise internal compass |
| 2 | Internal-IMU fusion | Internal compass |
| 3 | Damped internal raw gravity | GPS course, otherwise internal compass |
| 4 | Internal inclinometer | GPS course, otherwise internal compass |
| 5 | Damped Bluetooth raw gravity | GPS course, otherwise Bluetooth compass/yaw |
| 6 | Bluetooth inclinometer | GPS course, otherwise Bluetooth compass/yaw |
| 7 | Bluetooth inclinometer | Bluetooth device-fused compass/yaw |

Unavailable or stale channels show no data, not a level attitude or a north heading.
GPS course is direction of travel, not necessarily vehicle-body heading. Raw gravity
is apparent tilt and includes acceleration/braking effects; it is not compensated
vehicle attitude. Bluetooth device-fused yaw is not certified heading ground truth.

**Options / Bluetooth** scans paired and unpaired Classic/BLE devices, including
BLE advertisements, for 20 seconds. The status bar shows elapsed time, device count,
completion, cancellation, or discovery errors. Stop or repeat a scan as needed.
Choose an explicit **OBD2 device** and **Sensor device (WT901)**; both identities
are saved across restarts, including devices not seen during a later scan.
Selecting **Automatic** restores the previous paired-compatible discovery.
The OBD adapter setting selects the validated protocol, not the physical device.
Explicit selections connect without requesting pairing; Windows consent or device
security may still require it. Unsupported endpoints and access failures remain visible.
Saving a changed device/protocol reconnects enabled live sources without restarting
the drive; replay never opens these live connections.

**Sources** groups GPS, OBD2 and **Sensors / Internal / Bluetooth**. The chosen
WT901 sensor is acquired independently of driving acquisition; Bluetooth display selection
is disabled until fresh readings exist. Discovery and reconnect attempts use a
three-minute window by default, configurable from 1 to 15 minutes on the dedicated
**Options / Bluetooth** page.
**Options / Sensors** edits independent internal and Bluetooth yaw/pitch/roll reference
frames, including reset to all zeroes. References compose rigid rotations and affect
raw/inclinometer display only; fusion's measured physical installation remains separate.
Bluetooth starts with a level, zero-offset reference, not an assumed 90-degree mounting.
Saved references are preserved: use **Reset mounting** to clear an old mounting assumption,
or **Capture current pose as zero** while parked. Capture uses the selected Bluetooth
gravity/inclinometer mode, and the 3D preview immediately reflects all three offsets.
The heading zero does not rotate the roll/pitch axes.

Selecting Bluetooth defaults the horizon to its **device inclinometer**, not
accelerometer-derived apparent tilt. The raw-gravity mode remains an explicit comparison.
This changes the display/comparison reference, **never the internal IMU input to fusion**.
The existing `obd-inputs.csv` companion additionally retains
optional `Comparison` metadata at canonical input boundaries: source selection,
Bluetooth reception time/raw readings/status, both reference frames, OBD speed/time,
and speed-derived longitudinal acceleration in m/s2. The acceleration uses the same
source-time differentiator as the odometer, not render timing. Missing/stale values
remain absent. Replay uses recorded comparison samples and frames when available,
never current live Bluetooth data or current mounting settings. Older companions
without this metadata remain readable and show no Bluetooth comparison.
Keep the whole recording folder.

### Background plausibility and conditional gravity renewal

Live fusion observes detached internal IMU/inclinometer, Bluetooth comparison and
fusion snapshots on a background worker at **at most 0.25 Hz**. The first service
compares original odometer speed changes with IMU integrals over the **same
1.5–2.5 second interval**, not a UI-speed derivative or differently timed sample.
Status tooltips and the debug log report contradictions, insufficient evidence,
and feedback decisions. Bluetooth inclinometer readings are diagnostics, never
gravity truth or a replacement fusion IMU.

After three distinct, agreeing windows, the service can propose a conditional
moving gravity reference: subtract odometer-derived forward acceleration from
specific force before interpreting inclination. Thus steady speed can expose an
incline/bias candidate, while propulsion must not become pitch. This is **not**
proof of road grade: a measured rigid installation, forward-aligned non-slipping
wheels, negligible heave/lateral motion and the declared speed/force/clock error
hypotheses are still required. Turning, roughness, excessive dynamics, missing
gyro, stale speed and discontinuities withhold feedback.

The fusion owner revalidates the original evidence, applies at most **0.5 degrees
per review** to an existing reference, and retains a declared **3-degree reference
error plus the remaining correction error and subsequent gyro drift**. An
unavailable reference can be reacquired only from qualified repeated evidence.
Unsupported uncertainty is never hidden by increasing confidence or forcing a
level horizon. Source/reset/mount changes invalidate feedback. No accelerometer
bias or physical mounting is learned or persisted.

`obd-inputs.csv` journals each submitted correction, policy and original support
at its exact canonical input boundary. Replay restores those commands rather
than running a wall-clock background learner; older recordings remain unchanged.
Keep this companion with the recording to reproduce conditional moving renewal.

There is **no 30 km/h cutoff**. Without renewal, the default gyro error bound grows
from 1 to 5 degrees in roughly 23 seconds even at zero measured rotation; turns
can exhaust it sooner. Without qualified OBD evidence, the engine deliberately
remains unavailable until a new supported reference exists. Pattern fusion does
not calculate a statistical confidence or a cumulative driving/learning score:
telemetry shows the conditional tilt bound, and the horizon omits an unquantified
percentage rather than displaying a misleading permanent 0%.

**Options / Display / Always on** uses .NET 11's `KioskModeManager` to prevent
display/screensaver idling by default. Turn it off to restore normal system behavior;
fullscreen remains controlled by the existing commands.

- Starts and stops a shared sensor-service lease.
- Selects Windows IMU/GPS, Windows IMU only, Windows IMU with serial NMEA GPS, or no sources.
- Consumes a declarative `IAsyncEnumerable<SensorEnvelope>` subscription.
- Displays a gauge-only dashboard with gravity-removed vehicle acceleration, fused attitude/heading,
  speed/trip/altitude, a full-height route map with a bounded zoom slider, animated telemetry
  read-outs, road-section classifications, and a Direct2D debug log.
- Starts and stops service-owned recordings in all three formats.
- Replays recorded semicolon-CSV parts through the same sensor-source interfaces at their original
  timing, with optional automatic continuation across numbered files.
- Includes a disk-backed recording timeline viewer/editor.
- Applies optional damping through UI-neutral presentation ViewModels consumed by the DirectX
  controls. Damping choices include 300 and 400 ms. Display, source, route, recording, and the
  last file-picker directories are remembered across restarts.
- The speedometer has one scale: km/h (0–160) or mph (0–100). Double-click or double-tap
  the gauge to switch the scale, large central speed, and bottom trip drum together.
  The distance unit (`km` or `miles`) is outside the drum. The altitude sparkline sits above
  the artificial horizon; attitude values are below it. The large speed is **fusion speed**
  unless fresh, enabled OBD2 speed is authoritative.
  An undamped receiver-reported speed appears underneath as `45 (GPS)`, using the same units
  and a font exactly 2 pt smaller. Missing receiver speed stays unavailable, not zero.
  Telemetry shows receiver km/h and **GPS 4-point km/h**, replacing its former mph readout.
  The compass legend uses aligned `Compass:`, `YAW:`, and `GPS track:` rows. Yaw and track
  compare fusion and raw values side by side; unavailable fusion never masquerades as raw yaw.
  The track fusion column uses the estimator's true-north heading; the raw column uses
  receiver-reported course, not a coordinate-window substitute.

**Tools → Options → Display → Force OBD Gauges on** keeps OBD instruments in the dashboard
without a connected adapter. This preference is remembered and applies immediately; it does not
enable acquisition or invent readings. Unavailable OBD values remain unavailable.

The **Raw Data** tab (formerly GPS motion) retains the GPS comparison and adds a separated raw
sensor table with **Internal Sensors** and, when connected, **Bluetooth Sensors** columns.
Rows show inclinometer pitch/roll/yaw first (degrees), accelerometer X/Y/Z next (g, including
gravity), then gyro X/Y/Z and magnetometer X/Y/Z. Missing gyro data remains `---`; fresh
Bluetooth readings never replace internal readings. The current WT901 notification does not
include magnetometer vectors, so those Bluetooth cells remain unavailable. MFX/MFY/MFZ are
magnetometer readings in microteslas; Altimeter is the platform altitude change in metres,
not GPS altitude.
Numeric columns align decimal points using the configured precision. Missing hardware/readings
show dashes. Magnetometer and altimeter are live-only diagnostics and remain unavailable in
replay because existing recordings do not contain them.

Animated telemetry read-outs use the form's base point size, preferring
CaskaydiaCove Nerd Font, then Cascadia Code and an installed monospace fallback.
The upper instrument row is 20% taller than the lower row (6:5).
The shared gauge library now owns the gauge design and three-source compass;
the sample no longer has its own compass renderer. The legacy `.sln` is retired;
use `Sensored.slnx`.
CarMode displays the actual submenu commands in the toolbar row and temporarily detaches
hidden root commands. Icon refreshes explicitly use the dashboard toolbar as their rendering
host, so changing damping, scale, or font does not require a hidden root item to have an owner.
**Sources → GPS** keeps **Serial NMEA receiver** as a selectable leaf. **COM port** (auto-detect, ports present at startup,
and the remembered port), **GPS type** (receiver constellation profile), and **Target GPS rate** are siblings under GPS,
so CarMode stays within four levels (`Sources > GPS > COM port > Auto-detect`). Accepted choices apply immediately, including during a recording, without restarting
IMU acquisition, fusion or the recording. A missing serial receiver retries 15 seconds after
each failed attempt during the first recovery minute, then every 30 seconds. A changed COM
selection cancels pending probing/waiting and waits for safe old-port release before opening
the new port. Release/restoration failure blocks further opens and remains visible.
GPS-only preview starts with the window and continues after drive Stop; a connected transport
is shown separately from a fresh valid fix. Replay suspends live GPS until historical
presentation is reset or live acquisition is explicitly started.
Automatic reconnection and return from replay are read-only: they do not reuse
earlier authorization to issue new receiver commands without a fresh stationary check.
Explicit Start retains its parked-vehicle confirmation.
Active profiles use WARP's shared receiver-consent prompt from either the toolbar or
**Options → Source**. **Options → Interaction** independently controls this prompt and
the parked-vehicle confirmation before Start/Restart; both are enabled by default.
Disabling the receiver prompt explicitly authorizes temporary commands for selected active
profiles. Passive mode remains read-only, and live-motion checks and receiver restoration
are never bypassed. Start while parked and keep the receiver connected.
The Interaction page also sets submenu return-to-root delays: 5 seconds after a leaf
command, or 15 seconds of browsing without a command, by default (each 1–300 seconds).
Entering another submenu or going Back starts a fresh browsing interval; another command
restarts the shorter interval. Root-level commands have no return timer.
In CarMode, submenu arrows and option checkmarks are drawn beside the icon. A reserved
right padding still keeps those marks from covering the symbol. Checkable source commands (including OBD2) and radio-style COM-port, profile,
rate, damping, and scale choices retain space even when unchecked. Original padding and normal
dropdown indicators return when CarMode is disabled.

**Sources → GPS → Target GPS rate** and **Options → Source** select Auto, 1, 2, 5, or 10 Hz.
Auto keeps fastest-acknowledged serial negotiation and requests 1 Hz from Windows Location.
An explicit rate is a live reconfiguration request, not an observed delivery rate:
serial configuration still requires an identified supported receiver, a non-passive profile,
authorization, and stationary-vehicle confirmation unless explicitly disabled in Interaction.
The status row reports measured
sensor/GPS cadence; live fix freshness and requested/applied receiver details remain in tooltips.
Selecting a target rate from passive mode selects **Current constellations (rate only)**:
WARP changes and verifies CFG-RATE without rewriting CFG-GNSS, then restores the original rate
on Stop. Explicitly selecting Passive still sends no receiver commands.

The live status row shows lifecycle/rates, active GPS identity and COM port, OBD2 device and
measured speed-report rate, then raw platform pitch/roll/yaw and raw receiver speed in km/h.
A spring separates these fields from the green OK mark or red clickable last-exception message.
At narrow widths, GPS/OBD identities and exception text share the remaining space rather than
overlapping the raw readings; tooltips and the exception popup retain the full details.
The exception popup closes on click, outside dismissal, or after 15 seconds; **Options → Display**
changes its timeout (1–300 seconds). Unknown raw readings use dashes, never invented zeroes.
Cleanup displays the current task and item number, retaining full progress and warnings in
the status tooltip and debug log. During cleanup, readable stage descriptions replace internal
stage keys, and GPS/OBD/raw fields temporarily yield their space to the cleanup text.
An acquisition error such as a missing COM port stays in diagnostics but does not imply that
an otherwise successful lease release leaked resources. Release failures and unverified GPS
restoration still block restart; replay reference traces still require fault-free acquisition.
Replay keeps its existing transport controls.
The instrument panel occupies 60% of the dashboard width (20% wider than its
previous 50% share); the map and associated panels use the remaining 40%.
Once OBD2 has connected, the horizon/compass column is 15% narrower than its usual
width (42.5% instead of 50% of the instrument panel); speed and acceleration use the
released space. Temporary disconnection keeps this OBD layout; disabling OBD2 restores
the equal-width instrument columns.
The RPM, coolant, and fuel/battery instruments scale together into their available height,
including short slots and enlarged dashboard fonts, without a forced scrollable minimum.

The speedometer replaces its needle/ticks with two configurable radial LED rings:
the outer ring follows the main speed and the inner ring follows the secondary readout.
Receiver comparison uses the saved blue GPS profile; OBD's other-unit comparison uses
the saved red profile. Default captions follow the displayed units/source; custom captions
are preserved. The three saved appearance profiles remain compatible with existing settings,
but only two rings are drawn.
Scale numerals receive another 2 pt increase (4 pt above the trip font), while the
trip drum is back inside the instrument panel. `InstrumentNumeralPositioning` offers
`OuterPositioning`, `InnerPositioning`, and `Invisible`; both samples default to the
inside border of the innermost LED circle, with a 5-logical-pixel numeral margin.
These settings are saved through the shared Display editor.
Outer tiles are 20% shorter radially than before (1.6:1 rather than 2:1);
secondary tiles retain their previous radial length and common tangential height. The rings
are packed with 1-logical-pixel radial gaps and a 3-logical-pixel outer corner inset,
scaled with DPI; tile height fits the tightest inner-ring spacing without overlap.
Both rings use 33 shared angular positions. The inner ring plots the secondary
numeric reading against the selected scale; when it shows the same unit as the main
reading, equal readings illuminate equal angles. Removing the third ring and shortening
the outer tiles moves the inner numeral boundary outward, giving the numerals a larger radius.
For radii below 240 logical pixels, the outer tiles progressively taper toward the
center, up to a 12% narrower inner edge at 180 logical pixels or less. Larger
instruments retain rectangular tiles; secondary tiles remain unchanged.
Tiles have five lit levels plus off, remain cumulatively lit, and overlap the
handoff: 4 → `[4,1]`, 5 → `[5,1]`, 9 → `[5,4,1]`, 10 → `[5,5,1]`.
LED steps floor fractions without rounding numeric readings.
**Options → Display** persists each ring's white/green/red/blue color,
curved caption (at most 15 characters) and caption padding (default 5 logical
pixels, DPI-scaled). Every caption character has its own curved position and rotation.
Switching display units keeps the outer ring tied to the main reading and updates
the inner ring to match the secondary readout.

The acceleration gauge caption is 1 pt larger than before and uses Cascadia Code
(the system monospace font when unavailable). Every numeric axis includes a sign,
including zero, to keep positive and negative read-outs aligned.
The acceleration rails are closely spaced, wide LED tiles on two planes tilted 70 degrees
into the scene: the nearest tiles are widest and both rails converge into the distance.
Their neutral boundary is at the circle centre, with eight rows for acceleration and eight
for braking at symmetric screen-space positions. Individual tiles use two thirds of the
previous width and four fifths of the row height; both rails sit farther outward to leave
the indicator ball's central lane clear.
Green lights toward the viewer for acceleration and red lights away for braking. Their extent
and leading-tile intensity use the same damped forward value as the gauge and telemetry,
without a second attack/release filter. A square-root driving response makes gentle acceleration
and braking visible; 0.5 g acceleration or 0.8 g braking lights 80% of the corresponding half-rail,
with headroom for stronger events. This LED scale is independent of the circle's G full scale.
The panels are implemented in the shared instrument, not in the sample:
`D2DAccelerationGauge.ShowLongitudinalLedPanels` is a Designer-editable option,
defaulting to true; disabling it leaves the readings, indicator ball and shock behavior intact.
**Options → Display** controls decimal places separately for acceleration, GPS coordinates,
speed, and other telemetry values (0–8). Acceleration defaults to two decimal places, as do
other telemetry values; GPS coordinates retain seven and speed retains one. Satellite counts
remain integers. These choices apply immediately on OK and are saved across restarts.
Speed uses Bahnschrift SemiBold SemiCondensed Oblique; attitude uses the upright variant.

During shutdown, the status strip, Debug output, and dashboard log show the current
cleanup substep. Pending operations report their elapsed wait every second alongside
recording rows and finalized parts, source state/lease and processed-sample counts,
fusion inputs/estimates, trace entries written and queued, independent GPS fixes processed and
positions retained, or dashboard envelopes consumed.
The trace queue count excludes the entry currently being written; zero queued entries does
not claim that flushing or manifest publication has finished. The delayed progress window
shows the same details and only advances its stage count when a stage actually completes.
Normal Stop also reports cleanup progress, and status tooltips retain the complete message.
Fusion setup and cleanup/wait dialogs inherit the dashboard font. The shared setup
dialog lives in `WarpToolkit.WinForms.Sensors.DirectX`, refreshes readiness without
restarting acquisition, and provides a scrollable measured-mount editor. Its action
row uses equal percentage-width columns and all-side-anchored AutoSize buttons.
Windows high-contrast mode keeps system status colors.

Raw acquisition, classification and raw recording belong to Core, vehicle estimation to
`WarpToolkit.Sensors.Fusion`, and general display math to Presentation. Sensored adds the
OBD acquisition coordinator, compact telemetry instruments, a measurement-time trip accumulator,
and a versioned OBD/input companion without changing the raw recording schema.

## Fusion readiness, measured installation and independent OBD2 speed

The banner below the gauges explains why horizon/vehicle acceleration are unavailable,
independently of green acquisition status. **Fusion setup** shows each quantity's support,
original gate reason, required action and current pattern decisions. Raw platform angles
are device diagnostics, not an available vehicle horizon.

The current default has **no complete measured mounting reference**. More driving or a
down-only gravity capture cannot supply it. The old session-lock/motion-learning choices
are disabled in Options and retained only for settings compatibility.

While parked, stop acquisition and finalize recording, then open **Fusion setup /
Measured physical mount**. Enter an independently measured installation identity,
revision, vehicle-forward and vehicle-down unit axes in the current device XYZ frame
(space-separated components with decimal points), and provenance including device/mount
identity, measurement method and uncertainty. No axes are prefilled or inferred. Save
for the next live session; a changed installation requires a new revision or identity.
The profile is persisted and included in the fusion configuration used by recording.
The editor is read-only during acquisition, replay and recording.

A measured mount alone still does not establish gravity: start while parked and obtain
a supported quiet stop with fresh original zero-speed observations and valid continuous
IMU/gyro measurements. Zero speed does not imply level road. Motion-based mount learning
abstains while physical speed/timing uncertainty hypotheses are unknown. Quantity support
is conditional, not validated road accuracy, and body pitch/roll are not road grade/bank.
The current live profile is never assigned to unrelated historical replay inputs.

Sensored also enables the speedometer's **Auto forward-acceleration LEDs** with four
distinct speed measurements. Differentiation uses original selected-source timestamps,
not repeated UI presentation times. Invalid speed, source changes and long gaps reset
the window. This speed-derived indication is independent of IMU/body mounting and is
not a substitute for the gravity-compensated vehicle acceleration instrument.

The existing **GPS source** menu is unchanged. The independent **OBD2** toolbar toggle remembers
its preference and searches for the already-paired BLE adapter selected in
**Options → Source → Paired OBD2 adapter**: **OBDLink CX** (default) or **AD20-13303**.
The adapter choice is also saved. Change it while parked, then toggle OBD2 off/on or
restart the app to apply it; an existing connection/recording is not changed by saving
options. AD20 requires a supported BLE diagnostic UART and ELM-compatible firmware;
unsupported endpoints or command responses are reported, not replaced with another adapter.
The OBDLink-only `STDI` identity check remains enabled for CX, not AD20. WARP never pairs devices
or disables GPS. Fresh valid OBD speed, including zero, is presented exactly with speed confidence
`1.0`; attitude confidence is still independently estimated. Missing, invalid, future-dated,
disconnected, or more-than-300-ms-old OBD speed returns to qualified receiver GPS speed, with
automatic recovery when fresh OBD data returns. The tooltip and source-transition log distinguish
the preferred source from the effective source. GPS travel direction remains distinct from
vehicle-body heading, which the current engine leaves unobserved.

Fusion accepts measurement-time GPS updates from 1 through 10 Hz, including fractional
and changing delivery rates. Delayed/batched fixes use historical IMU coverage rather than
the amount of IMU data received between GPS arrivals. Exact OBD speed constrains both
the velocity and its covariance, avoiding false attitude corrections from already-known
speed. Magnetic heading, fused vehicle yaw and platform device yaw remain distinct:
telemetry and the compass share normalization, confidence validation and yaw-source selection.
Magnetic heading is never relabeled as vehicle/device yaw.

Vehicle acceleration becomes available once the mounting axes are observable and remains
independent of GPS cadence. Until then, the acceleration gauge explicitly explains whether
it is waiting for fusion data, mounting gravity calibration or vehicle forward-direction
learning. It never substitutes raw gravity-containing device acceleration or fabricated zeroes.

With OBD2 selected, WARP's shared `ObdLinkConnection` starts paired-adapter discovery immediately,
independently of IMU/GPS startup. A saved preference starts discovery when the form opens.
WARP explicitly requests Windows association-endpoint pairing metadata during scans and
watching; a missing default pairing projection must not hide an already-paired adapter.
The initial search window starts with discovery and lasts 10 minutes by default,
configurable from 1 to 15 minutes in **Options → Source**. Selecting OBD2 later starts a fresh window.
A device found during acquisition or recording becomes the speed source
on the next fusion input without restarting either pipeline; the recording companion preserves
that transition for replay. Expiry stops discovery only if no adapter has been established.
Once connected, the same paired adapter is retried directly after a disconnect, including
after the initial window expires, until OBD2 is disabled or the app closes. No device watcher
runs alongside an active connection, and reconnect does not rescan other adapters.

One serialized, read-only ELM session prioritizes speed, interleaving supported RPM, coolant
and fuel polls. Speed targets a 50-ms cycle, but actual cadence depends on adapter/ECU response
time. The 50-ms target is not a 200-ms response deadline: mixed polling allows the normal
five-second command timeout, so ordinary BLE/ECU latency does not tear down a healthy session.
Absent replies still time out and reconnect instead of blocking indefinitely or
reusing late replies. Disconnects trigger a five-second targeted retry delay; terminal cleanup errors
are surfaced and cannot be replaced by a new connection in the same app instance.

Once OBD has connected, three stacked instruments occupy the left half of the
existing map content area; the map keeps its zoom control and takes the right half. Temporary
staleness or disconnect leaves the column visible with unavailable readings and a reconnecting
status; GPS/inertial speed remains the freshness-gated fallback. Only deselection restores
full map width, and map-off hides both. The three instruments share one
`D2DVehicleGaugePanel` render host, with separate retained `D2DVisual` content surfaces.
All drawing, including the large italic readouts, goes through Direct2D/DirectWrite.
Antialiased cumulative tiles follow a horizontal rail blending smoothly into an upward arc.
Fixed numeric columns keep units stationary as the digit count changes.
RPM (`1234 RPM`) is green through 5,000, yellow through 6,000, then red, independent of the
selected 4,000/6,000/8,000/10,000/12,000 RPM scale. The panel retains the
keyboard-accessible scale context menu. Coolant displays both Celsius and Fahrenheit;
its rail is green through 99 Celsius, yellow through 110 Celsius, then red.
Fuel and battery have independent stacked rails: light gray fuel above yellow battery,
with a combined percentage readout. Missing readings use `---` and dim, unlit tiles.
Traction-battery state-of-charge remains unavailable until a verified vehicle-specific source exists.
Generic PID 5B and voltage are not battery state-of-charge. Trip distance is integrated once on
the full-rate fusion owner using measurement time, remaining continuous across speed-source changes.

### Reusing the Direct2D instruments

The source-neutral types live in `WarpToolkit.WinForms.DirectX.Gauges`:
`D2DRpmGauge`, `D2DTemperatureGauge`, `D2DFuelGauge`, `D2DBatteryGauge`, and
`D2DFuelBatteryGauge` are individual controls. Set `Value` in RPM, Celsius, or percent;
the dual instrument uses `Value` for fuel and `BatteryValue` for battery. Supply `double.NaN`
for unavailable or stale data. The numeric readout is not clamped; the tile extent is.

For a custom dashboard, create `D2DGaugeVisual` models, add visuals to one `D2DControl`
or `D2DForm`, and attach each model to its visual:

```csharp
using D2DGaugeVisual rpm = new(D2DGaugeKind.Rpm);
D2DVisual visual = host.Visuals.AddNew(new Rectangle(0, 0, 360, 144));
rpm.Attach(visual);
rpm.Value = 1200;
```

Keep the model alive for the dashboard's lifetime. Change the visual's `Bounds` on layout.
`PaintContent` redraws automatically after resize, render-mode changes, and device recovery.
Models synchronize reading updates and rendering for classic, shared-thread, and
dedicated-thread hosts. Call `Detach` before removing a visual; disposing a model detaches
all its handlers but does not dispose host-owned visuals. Models do not acquire telemetry.
The old Specialized `LedTileGauge` remains as a Direct2D-backed compatibility adapter.

### OBD/input recording companions

New live sessions maintain a temporary disk-backed input journal (bounded in-memory queue).
Finalizing a raw recording exports `obd-inputs.csv`, version 2, into that recording's folder.
It contains exact original envelopes, sequence/order, source/arrival times, applied OBD snapshots,
and mounting-reference transitions, including the causal session prefix before recording began.
That prefix permits deterministic filter warm-up for mid-drive recordings and seeks without
serializing private estimator internals. This adds disk usage proportional to session duration;
the temporary journal is deleted on normal session cleanup, and each exported recording includes
its own prefix. Do not remove companions when moving a recording. Service-owned raw ZIP
mirrors do not include this app-owned companion: retain/copy the recording folder and
`obd-inputs.csv` for exact OBD replay, not only the raw ZIP.

Replay streams the companion with bounded memory, restores original timing and order (including
duplicate timestamps), and warms the estimator from its session prefix before the selected input.
Playback is paused during initial warm-up; contiguous parts retain the same cursor and estimator.
No live Bluetooth data enters replay. Old recordings without companions remain GPS/IMU-only;
unsupported/corrupt companions and `.pending`/`.writing` files produce visible incomplete-recording
errors instead of silently mixing sources. Disk/queue failures prevent publication of a complete
companion. Finalization waits for fusion to cover the raw recording's relevant final input.

Synthetic regressions cover signed 5/10-degree stopped slopes for 60 seconds, flat and genuinely
banked/graded turns, delayed GPS and OBD fallback/recovery. Controlled road recordings and actual
Visual Studio Designer/DPI round-trips remain necessary hardware/interactive acceptance checks;
a stationary bench cannot validate real driving dynamics.

## Independent GPS speed and direction

The **GPS motion** tab compares fused speed/heading, the receiver's reported speed/course,
and coordinate-derived speed/course over the latest **2, 4, 10, 15, and all retained positions**.
History is bounded to **32 distinct measurement-time fixes**. Its own unthrottled subscription
and sequential background consumer keep calculations independent of dashboard rendering,
visual pause, IMU availability, and fusion success; it is not a UI-timer estimator.

Each coordinate speed is the sum of geodesic segment lengths divided by the window's
coordinate-measurement time span. Two fixes give the fastest but noisiest interval average;
four fixes smooth it slightly; longer windows smooth more and necessarily lag changes.
They are not instantaneous speed measurements or accuracy guarantees: stationary position
jitter can inflate coordinate speed. Direction is a distance-weighted true-north track bearing,
unavailable below 5 m of motion or when opposing segments cancel. The compass prefers
receiver course and otherwise uses the four-position track.

The tab shows actual fix counts, window durations, rejected-fix counts, and the latest
rejection/restart reason. Duplicate/out-of-order measurement times, invalid coordinates/no-fix
reports, and coordinate jumps above 360 km/h do not contaminate accepted history.
Source changes, transport gaps, unrelated sessions, and seeks restart the history; contiguous
recording parts preserve it. Measurement gaps over 10 s also restart windows rather than
averaging across unobserved travel. Replay uses recorded measurement time for calculations
and real arrival age adjusted for playback pacing for freshness. Stale values are unavailable;
pausing replay preserves current readouts, and a completed replay retains its final comparison.
The extra reader drains on Stop, including start failures and replay transitions, with its own
cleanup stage and processed-fix counter. **Options > Display** applies speed/other numeric
precision to the comparison grid as well as telemetry.

## Serial GPS receiver

Serial NMEA is **passive by default**: no receiver commands are sent. In Options → Source, select
a GNSS profile (GPS/QZSS, BeiDou, Galileo, GLONASS, or GPS/BeiDou/QZSS) and explicitly authorize
temporary receiver commands **for this session** to opt in. The profile is saved, but authorization
is not; selecting a profile alone never writes to the receiver. A warning explains that unplugging
the receiver or terminating the process can prevent restoration. With authorization, the source
first polls UBX MON-VER and both CFG-RATE and CFG-GNSS. Only identified, supported u-blox
receivers with valid configuration readback are changed. The session sends volatile CFG-GNSS and
CFG-RATE changes (never CFG-CFG/flash), checks ACKs and readback, and restores the original
payloads when acquisition stops. Dynamic negotiation tries up to 10 Hz for every profile,
including multi-GNSS, and retries unsupported rates more slowly. Actual valid RMC fix cadence is
measured separately from the requested configuration; serial throughput, sky conditions, and
receiver firmware can reduce it. `SerialNmeaGpsSource.ReceiverStatus` and `MeasuredFixHz` expose
the negotiation/restore result and observed rate to source consumers. If restoration fails,
the source reports a fault and the receiver may retain volatile changes until reboot; verify its
settings before another run.

Windows Location is a distinct provider. Its existing default requests a 1,000 ms
geolocator report interval; `WindowsSensorSourceOptions.GpsReportIntervalMilliseconds`
now permits a 100–60,000 ms request. A 100 ms request is **not** a promise of 10 Hz:
Windows and the location hardware may deliver fewer fresh positions. Sensor-service
subscriptions can pass separately timestamped 1, 5 and 10 Hz fixes without a 1 Hz
software cap; display cadence, requested rate, and measured valid-fix cadence are
different quantities.

Unidentified receivers are **not** configured. Per-session authorization permits a bounded
read-only UBX identification attempt on a serial NMEA device; if the model is not identified as
supported u-blox hardware, the source reports the unsupported status and continues reading NMEA
without changing receiver settings. No generic or other-vendor constellation/rate command is
implemented: vendor protocols and restoration semantics must be verified before adding one.

On a COM5 u-blox 8 (SPG 3.01, protocol 18.00), a bounded live test measured approximately
1.00 Hz passive, 4.91 Hz mean/5.00 Hz median for GPS + BeiDou + QZSS, and 9.82 Hz
mean/10.00 Hz median for GPS/QZSS only. The original volatile CFG-RATE and CFG-GNSS
payloads were read back byte-for-byte after **each** stop. This receiver omits the `MOD=`
MON-VER extension; identification also accepts its exact verified M8 hardware/firmware/
protocol tuple. A CFG-GNSS restore ACK was occasionally not observed even though readback
confirmed the exact original bytes; the source reports this distinction rather than falsely
claiming a failed restore. No nonvolatile configuration was saved.

The newer [COM8 measurements](..\FusionGauges\GPS-Frequency-Tests.md) also observed
10 Hz with GPS + BeiDou + QZSS. Both samples' actual independent processing owners
received 93 fixes in one ten-second live window, without subscription/ingress drops.
These are bounded measurements, not a guarantee under other reception conditions.

The dashboard enables `SymbolToolStrip.CarMode`: parent commands replace the toolbar
row instead of opening popup menus. Back/Escape/Alt+Left restore the preceding level;
the SVG Go to Root button or Ctrl+Home cancels navigation. The heading uses the strip
font +2 pt, semibold/bold, and the background is 20% lighter in dark mode or darker in
classic mode. Original command instances, checked state, handlers and layouts are
restored. Four effective menu levels, counting the root row, are supported; a fifth
visible level is rejected with the offending item path. Submenu commands have their
own symbols, including SVG variations for ordered display and replay settings.

Use **Options → Source** to select the source, serial COM port, passive or authorized
receiver profile. Saving options or choosing the GPS source menu does not restart
acquisition or change the receiver. Use **Start** for a stopped session or **Restart**
to finalize the current recording, release the previous session, and apply the selected
source. Auto Recording starts a new recording only when enabled.
Temporary receiver commands still require per-session authorization and a stationary
vehicle. If fresh navigation cannot establish that the vehicle is stationary, stop
acquisition while parked before starting an authorized receiver profile.
The status strip distinguishes the selected and running source, link readiness and
accepted fix age, rejected fixes, and measured versus receiver-readback rate. A Ready
serial link without a valid fix is **not** a position fix. Profile selection does **not**
prove which satellites contributed to any fix.
If COM5 continues outputting RMC with status `V` and GGA fix quality `0`, the receiver
is streaming but has no usable satellite position; the dashboard cannot create a fix by
changing the requested rate. While parked, check clear sky and the antenna/cable, then
allow time for acquisition. A reported antenna status of `OK` confirms only the
receiver's electrical diagnostic, not a usable satellite signal. If needed, select
Windows Location as a separate provider; it does not make COM5 acquire satellites.
The main status strip `?` opens one nonmodal local D2D Markdown help window; requesting
another topic replaces its text rather than opening another viewer.
During replay, the status strip reports playback cadence but marks live receiver diagnostics
and fix age unavailable. Older recordings omit the newer receiver fields; recorded arrival
timestamps must not be compared to today's clock to claim that a replayed fix is stale.

GPS, Galileo, GLONASS and BeiDou have global coverage; QZSS mainly serves
Asia–Oceania and WAAS North America. The previously measured ~5 Hz multi-GNSS and
~10 Hz GPS + QZSS rates are examples for the tested receiver, not universal rates.
In **Options → Map / route**, online OSM tiles require explicit permission; when disabled
or unavailable the route and current-position marker still display without a basemap.
Windows 11 downloaded offline map packages cannot supply tiles through this WinForms
map API. The Map menu reports tile status and offers a retry for failed online tiles.

## Shutdown diagnostics

Closing the dashboard first finalizes its recording, releases the sensor lease and temporary
receiver configuration, then drains fusion and dashboard updates. A delayed compact progress
window shows completed cleanup stages, the active stage and elapsed time; elapsed time does
not advance completion. If cleanup does not finish promptly, **Keep waiting** extends the wait; an
explicitly confirmed **Force Exit** can leave the last recording part unfinished or the receiver
temporarily configured. Recording errors do not skip attempts to release independent sensor
sources. Retry is offered only while a failed step is still safe to repeat; a completed one-shot
release fault or unverified receiver restore remains incomplete and offers **Return to app**
or confirmed **Exit anyway**, never false success. A forced exit is recorded as
`run-stop-after-incomplete-cleanup`, not a normal `run-stop`, in
`%LocalAppData%\WARP\Sensored\CrashReports\warp-lifecycle.jsonl`.
If the receiver reports that its original volatile settings were **not verified restored**,
normal Exit and new acquisition are blocked for that app run. Return to inspect the
warning or choose confirmed Force Exit, then check the receiver before another run. The
terminal source fault is retained even though health subscriptions detach before receiver
shutdown.

The lifecycle log records shutdown stages, failures and decisions without GPS coordinates or
raw NMEA sentences; stage markers mean **entry**, not successful completion. A local
`warp-crash-*.txt` report preserves the full exception chain for a failed cleanup, while
the dashboard debug log records source kind, source-release duration and service state.
`shutdown-closing` and `shutdown-form-closed` distinguish a completed cleanup from a
failure to finish app/host exit. A missing normal exit does not by itself identify a failed
GPS receiver. If a hang recurs under a debugger, capture the application hang separately
before stopping the debugger; a Visual Studio crash is not automatically an application crash.

A saved non-passive GNSS profile without per-session authorization leaves the receiver unchanged.
This is reported by receiver configuration diagnostics, not as an acquisition fault, and does not
prevent a clean shutdown of otherwise healthy passive acquisition.

## Threading

The service creates one dedicated sensor-processing background thread for every active session, even
when only one or no hardware source is selected. Source callbacks enqueue only. The UI consumes its
subscription asynchronously and uses `Control.InvokeAsync` before updating controls.

Slow subscribers do not block sensor processing. Their bounded queues drop the newest item and expose
drop statistics and explicit gap envelopes.

Fusion has its own unthrottled subscription and sequential background consumer. It never awaits UI
rendering. The display consumes latest immutable estimates; fusion and trace production run at the
full IMU rate (about 400 Hz in the reference recording). A trace writer failure or any transport loss
is reported explicitly and prevents the output from being labeled complete.

## Values

The artificial horizon and acceleration gauge consume automatically calibrated vehicle-frame
estimates, not raw device axes or the Windows inclinometer. Compass and speed also consume fusion
output. Pitch is positive nose-up, roll right-side-down, acceleration forward/right/down, and heading
clockwise from true north. Engine acceleration is m/s2; the demo converts it to g for the existing
gauge. Calibration requires ordinary observable driving, not a button or a prescribed maneuver.

GPS numerical readings, the recorded route/trip/altitude, and classification remain
raw/provider-based. The telemetry footer identifies fused position, calibration state,
confidence, and rejected observations. No raw recording values are replaced with estimates.
Before fusion has established the mounting axes, the acceleration gauge and artificial horizon
are unavailable rather than showing gravity or device orientation as vehicle motion. The telemetry
tab can still show separately labeled, gravity-included raw device XYZ for diagnosis. Navigation
and its speed display become unavailable when the last accepted GPS fix is too old; an old
position must not make an unconstrained estimate look current.

Serial GPS and Windows IMU measurement clocks must agree. GPS fixes more than two seconds ahead
of navigation time are rejected as `gps-clock-ahead`; fresh arrivals and a Ready serial connection
do not override that check. If every GPS fix is rejected this way, check Windows date/time
synchronization against the receiver's UTC clock. The dashboard Debug log warns when live serial
GPS enters this ahead-of-PC condition and reports when it returns within the pending-fix limit.
Do not widen the pending-fix gate or substitute arrival timestamps for measurement time to conceal
a clock offset. Raw recordings retain both source and arrival timestamps so this condition can be
diagnosed without changing recorded data.
The status-strip IMU/GPS numbers are cumulative sample counts, not queue lengths.

The Windows **standard** accelerometer includes gravity and movement; the **linear** and
**gravity** types are separate processed sensor streams in the diagnostic app. Fusion
requires the timestamped standard acceleration as specific-force input and must not
substitute the gravity-removed linear value without changing its measurement model.
Long periods without GPS do not, by themselves, erase a learned mounting. If the tablet
is physically moved in its mount during such a period, however, the new vehicle-forward
axis cannot be inferred while stationary; a subsequent drive with suitable GPS/IMU
excitation must recalibrate it. A **Converging** state must not be interpreted as a newly
verified installation.

The **Damping** setting is presentation-only. It never changes service envelopes, recorded rows,
replay data, or classifier inputs. **Pause** freezes dashboard visuals only; acquisition and
fusion/recording continue. Unavailable or unobservable estimates are not presented as valid fixes.
Gauge damping and shock animations use the current presentation clock, matching the controls'
render clock. The engine and trace retain original measurement time; feeding historical replay
dates to render-time shock decay would otherwise make new shocks disappear immediately.

### Indicator directions

The acceleration ball shows the **inertia an occupant feels**, not the raw acceleration vector.
Accelerating pushes you back, so the ball moves **down**; braking moves it **up**. A right-hand
curve throws you left, so the ball moves **left**, and a left-hand curve moves it **right**. The
printed values keep their vehicle-frame signs: forward positive for acceleration, lateral
positive to the right.

Both perspective rails show the same longitudinal state: yellow neutral pavement, a G-proportional
green fill **toward the viewer** for acceleration, and a red fill **away from the viewer** for
braking. Fill grows from the midpoint and changes smoothly; it is not a scrolling speed animation.
Unused pavement stays yellow.

The horizon uses vehicle attitude, not acceleration as a substitute for pitch. Positive nose-up
pitch moves the horizon down; positive right-side-down roll rotates the horizon counterclockwise
behind the fixed vehicle marker. Real bank and grade must not be forced to zero.

## Recordings

Recordings are created below:

```text
%LocalAppData%\WARP\Sensored\Recordings
```

Every request creates a separate folder. The service writes a globally ordered event stream in:

- verbose JSON Lines;
- compact schema-header JSON;
- semicolon CSV with invariant numbers and conditional per-field quoting.

Parts split at wall-clock full-minute boundaries or the configured maximum row count. Finalized file
names contain the dominant road-classification abbreviation. Each part has a JSON classification
sidecar, and the folder has a recording manifest.
Version 3 recording health rows also retain coordinate-free GPS link/configuration, last-valid-fix
arrival, measured rate, invalid-fix reason and fix-quality evidence. The reader remains compatible
with existing version 2 recordings; missing historical diagnostics are shown as unavailable, not
as a live receiver disconnect.

### Recording timeline and editor

Open the recording viewer from the dashboard and select a **completed recording folder** to
inspect a 250 ms time-aligned timeline. Its virtual grid shows part boundaries, source rows,
speed, raw GPS course, fused heading/yaw, pitch, roll, and forward/lateral/vertical g with
units and availability. Raw sensor rows remain inspectable separately. The timeline index
is disk-backed and deleted when the viewer closes or a load is canceled, so multi-million-row
recordings need not be held in UI memory.

The editor suggests intervals longer than a minute with slow, position-stable GPS fixes and
quiet IMU data; it refuses to infer a stop across a long GPS outage. Each suggestion keeps
five seconds at both ends. Select suggested cuts or specify manual time ranges, inspect the
preview, then **Save As** to a new folder. Removed intervals compress the derived timeline:
arrival, sensor-source, GPS-coordinate, and GPS-fix timestamps shift consistently. Original
service sequence numbers remain for gap provenance, while output rows are renumbered.
Derived classification envelopes are reclassified into new part sidecars.

Saving rebuilds every format listed by the source manifest, part headers, classifications,
and manifest, records the source manifest SHA-256 and removed ranges, reads the generated
CSV back, and publishes the new folder only after validation. Existing destinations,
active/incomplete recordings, invalid ranges, and inconsistent manifests are rejected.
Neither source files nor source metadata are overwritten or deleted; only redundant
**output** parts disappear during regeneration. Compressed timestamps describe an edited
recording, not the wall-clock history of the original measurements.

## Replay

Choose **Replay > Open recording...** on the main toolbar and open a semicolon-CSV part file produced by a
WARP sensor recording. The dashboard stops any running live session, starts the selected file
immediately, and honors the recording's arrival timeline at the selected speed. The same menu offers
**25%, 50%, 100%, 150%, 200%, and 300%**, defaults to 100%, and remembers the selection.
Changing speed during replay preserves the current position, pause state, and fusion session.
Replay pacing uses arrival
timestamps because IMU and GPS source timestamps come from
independent clocks and can legitimately interleave out of order. Emitted samples retain their
original source timestamps. One monotonic clock schedules absolute deadlines, so individual timer
wake-up delays do not accumulate at every IMU sample. GPS measurement intervals remain an independent
timing reference, not a required one-second playback heartbeat.

The **Replay** page under **Options** can detect names such as `Part1`, `Part2`, and WARP's
`part-0000` files. Contiguous parts are ordered numerically and each continuation is checked for
the recorded arrival timeline, row/sequence continuity, valid GPS coordinates, and plausible GPS distance.
At the natural end of a valid part, the next part starts automatically without clearing the route
or final gauge state. A missing, malformed, overlapping, or implausible part stops continuation and
is reported in the debug log.

Replay uses `SensorReplayPlayer` as the session's `IImuSource` and `IGpsSource`; the ordinary
`ISensorService` subscription therefore drives the acceleration gauge, artificial horizon, compass,
speedometer, telemetry, route map, health, and classification pipeline exactly as live acquisition
does. Auto-recording and the Record command are disabled during replay.

At the natural end of the file, the service drains its pending sensor and UI queues, stops the
session, and leaves the final gauges and route visible. A later normal **Start** returns to the
configured Windows or serial source. Explicit **Stop** resets the replay presentation in the same
way as a stopped live session. Invalid or unreadable CSV data is reported in the dashboard status,
debug log, and exception history.

After a replay file is selected, the status strip shows Play/Replay, staged Stop, 5- and 30-second
back/forward navigation, Pause, a `{mmm:ss:ff}` timeline menu, and the current file/part count. The
timeline menu jumps to 20%, 40%, 60%, 80%, the first or last GPS coordinate change, or the start.
The first Stop preserves the current display and position; a second resets the current part; a third
returns to the originally selected part and time. Double-click the file label to select the file in
Explorer. Transport actions use embedded, theme-aware SVG symbols with tooltips and keyboard
activation; time and file information remain text. Pause changes to a Resume symbol while paused.
While replay is active the Play button flashes softly. Seek intervals always refer to recording
time, independent of the selected speed.

Seeking and replaying restart fused navigation, because recorded time jumps. They do **not** rebuild
the device-to-vehicle mounting calibration, which seeking cannot invalidate: the same device stayed
in the same mount for the whole recording. Attitude, acceleration and heading therefore keep reading
immediately after a seek, instead of leaving only the GPS-anchored speed alive for the next minute
of driving. Opening a different recording, or switching between replay and live acquisition, still
starts an entirely new estimator. A GUI trace segment written after a seek is a partial drive that
begins with retained calibration; the deterministic `fusion-reference` export remains the
reproducible baseline.

With **Hide replay controls** enabled, the full transport stays hidden. Moving the pointer over the
status strip reveals only Play, Stop, Pause, and the timeline menu; the replay file label remains
visible.

## Recorded data processor

`WarpSensorsDataProcessor` is a cross-platform command-line companion project in the
`Sensored` solution. It reads finalized `part-*.csv` files from a recording folder and
always writes new output; source recordings are never changed.

```powershell
dotnet run --project .\WarpSensorsDataProcessor\WarpSensorsDataProcessor.csproj -- `
  --recording-folder C:\Recordings\drive-20260915 `
  --damping-ms 275 `
  --trim-stationary-end `
  --merge `
  --output C:\Processed\drive.csv
```

The processing operations can be used separately or together:

- `--damping-ms <100-2000>` applies deterministic exponential damping to recorded IMU
  acceleration, angular rate, attitude, and heading values and to GPS coordinates, altitude,
  speed, and course. The requested time constant is rounded upward to the next 50 ms; for example,
  `275` becomes `300`.
- `--merge` orders CSV parts by their numeric part index and writes one schema-compatible CSV file.
  Without this option, processed files retain their original part names in a new output directory.
- `--trim-stationary-end` detects a final run of valid GPS coordinates that remains within a 15 m
  jitter radius. If that run exceeds one minute, it keeps its first minute and removes every later
  row, including IMU, health, gap, and classification rows.

`--recording-folder` is required, and at least one processing operation must be selected.
`--output` is optional: it defaults to `<recording-folder>\processed.csv` with `--merge`, or to the
new directory `<recording-folder>\processed` without it. Existing output paths are rejected rather
than overwritten. Use `--help` for the complete command reference.

## Golden fusion reference

### Independent replay timing

The `replay-timing` command measures actual callback delivery through the replay source and
unthrottled service pipeline against independently read input timestamps. It does not infer
real-time speed from the player's position or the historical timestamps of fusion outputs.

```powershell
dotnet run --project src\Sensored\WarpSensorsDataProcessor -- `
  replay-timing --recording-folder src\Sensored\ReferenceData `
  --start-seconds 0 --duration-seconds 60 --speed 1.0 `
  --output C:\WARP-Reference\replay-timing.json
```

Add `--fusion` to measure the actual Golden engine's full-rate processing as well as transport.
Supported UI rates correspond to multipliers `0.25`, `0.5`, `1.0`, `1.5`, `2.0`, and `3.0`.
The diagnostic stops at the first recorded row reaching the requested window end, drains every
selected input, and reports the actual measured span. A window without sufficient recorded data
fails explicitly. No hardware, GUI, or map network is involved.

The versioned JSON records source hashes, runtime/engine identity, actual monotonic observation
offsets, selected-rate segments, startup/drain time, lateness, consumer latency, and loss counters.
GPS elapsed time is a separate cross-check, not the replay scheduler. Exit code 0 means complete
evidence within 5% of the requested rate; code 1 means a timing or execution failure; code 2 means
invalid arguments. Existing output paths are rejected. An incomplete execution never publishes
successful-looking evidence. This diagnostic does not write a fusion CSV; the numerical export
below remains intentionally unpaced. Its version 2 trace has 72 columns, retaining the original
62-column prefix and appending coordinate-free mount calibration evidence.

### Numerical reference export

The supplied baseline is `ReferenceData` (25 original CSV parts). The benchmark contract is
`src\WarpToolkit.Sensors.Abstractions\Fusion\IFusionEngine.cs`; output units and signs are defined
by `FusionEstimate.cs`. See `FusionReference\TRACE-SPEC.md` and `FusionReference\NOTES.md` for
the schema, filter assumptions, verification results, and reproducibility details.

From the repository root, use the existing portable data processor:

```powershell
dotnet run --project src\Sensored\WarpSensorsDataProcessor -- `
  analyze --recording-folder src\Sensored\ReferenceData `
  --output C:\WARP-Reference\analysis.json

dotnet run --project src\Sensored\WarpSensorsDataProcessor -- `
  fusion-reference --recording-folder src\Sensored\ReferenceData `
  --output C:\WARP-Reference\baseline
```

Choose new output destinations; originals are never overwritten. Reference export uses measurement
time without real-time waiting and writes every IMU output, not just UI frames. Analysis and
exploratory processing are separate commands: the baseline is never silently damped or trimmed.

GUI replay also writes a full-rate trace beneath
`%LocalAppData%\WARP\Sensored\FusionTraces`. Continuous numbered parts share estimator state
and one trace. Pause/resume preserves state; seeking, restarting, or changing sources starts a new
calibration/trace segment. Completed traces are published only after acquisition, fusion, and writer
queues drain; `.writing` artifacts and incomplete manifests must not be used as golden references.
Live acquisition uses the same engine but does not automatically save a fusion trace.
Explicit Stop, seek, source changes, shared-service cutoffs, and shutdown retain `.writing` CSV
and `.incomplete.json` diagnostics rather than publishing a completed golden drive. Only natural
end-of-sequence with verified terminal acquisition counters publishes the completed CSV/manifest.

Raw measured route history/trip/altitude remains independent of fused navigation. No GPS altitude
is used to infer pitch. An unannotated drive is useful for consistency checks, but cannot establish
absolute grade truth; the numerical tests use known synthetic motion as well.

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

## Diagnostics

Unhandled startup, UI, background, and task exceptions are written below:

```text
%LocalAppData%\WARP\Sensored\CrashReports
```

Each failure produces a timestamped `warp-crash-*.txt` report with the exception and startup stage.
`warp-lifecycle.jsonl` records the corresponding process lifecycle and failure events.

## Deterministic validation

The library and demo are validated through fake sources, replay, and file-I/O tests. No physical
sensor array is required. During active Release use, build and test the Debug configuration only.
