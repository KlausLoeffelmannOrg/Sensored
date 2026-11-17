# Golden reference: implementation and reproduction

## Delivered implementation

`WarpToolkit.Sensors.Fusion.GoldenFusionEngine` implements the contract in
`src\WarpToolkit.Sensors.Abstractions\Fusion` in WARP. The portable library targets .NET 10 and .NET 11.
The demo consumes an independent, unthrottled fusion subscription before UI coalescing; its
controls retain a separate presentation boundary. Raw service envelopes, recording formats, classification,
measured route/trip/altitude, and the raw-sensor diagnostic view remain independent.

The implementation is causal: no future recording rows, route-specific constants, GPS altitude,
or platform-fused inclinometer attitude are used to determine vehicle motion.

## Frames, units, and filtering

Navigation uses north/east/down, with gravity `(0, 0, +9.80665)` m/s2. Vehicle axes are
forward/right/down. Internally, a double-precision quaternion rotates device vectors into
navigation coordinates; a separately learned mounting rotation supplies the vehicle axes.
Euler angles are computed only for output: pitch positive nose-up, roll right-side-down,
heading clockwise from true north.

**The recorded Windows acceleration is not inertial specific force.** It points along gravity
at rest. The conversion is `fDevice = -9.80665 * rawAcceleration`; angular rate converts from
right-handed degrees/second to radians/second. This polarity was checked independently against
GPS kinematics: a robust raw-axis regression over 1,343 drive intervals gave a handedness
indicator of +0.977 for gravity-aligned raw data. The raw contracts now document the convention.
Other sensor conventions must be adapted before entering this benchmark interface.

The nominal filter state is quaternion attitude, navigation velocity/position, gyro bias, and
accelerometer bias. Its 15-element error state is `[attitude, velocity, position, gyroBias,
accelBias]`. Propagation uses `aNavigation = R * (fDevice - accelBias) + gravity`; covariance
uses `Phi * P * PhiTranspose + Q`, not the non-PSD-preserving Euler covariance shortcut.
Measurement updates use innovation gates, Joseph covariance updates, and an attitude-error
reset. The initial heading/tilt covariance is rotated into the actual device frame.

Mounting calibration combines robust low-dynamic gravity windows with interval-aligned GPS
speed/course evidence. Excitation, residuals, angular uncertainty, and stability determine
calibration; elapsed time alone never does. Mounting is kept separate from the navigation
error state to avoid jointly fitting otherwise confounded tilt and bias.

Gravity corrections are gated by current IMU dynamics as well as GPS acceleration. This
protects the start of braking before the next 1 Hz GPS fix. Standstill and vehicle-motion
constraints have their own bounded update cadence. The actual magnetic input is a scalar
heading plus confidence, not a magnetic vector: its alignment to vehicle/true heading is learned
from usable moving GPS course, with repeated observations and interference handled explicitly.

Transient implausible IMU samples hold the previous displayed estimate and set `RejectedSample`.
Persistent *soft* baseline changes may be reacquired, but persistence never overrides the
absolute force/rate bounds. A sustained 20 g fault must not become accepted after the
consecutive-rejection budget.

### Principal defaults

The manifest serializes **every effective option**; `FusionOptions` and that manifest are the
authoritative full parameter list.

| Parameter | Default |
| --- | --- |
| Gyro / accelerometer noise density | 0.0006 rad/s/sqrt(Hz); 0.06 m/s2/sqrt(Hz) |
| Gyro / accelerometer bias random walk | 0.0002 rad/s/sqrt(s); 0.0005 m/s2/sqrt(s) |
| Maximum integration step / IMU gap | 0.02 s / 2 s |
| Gravity update cadence / noise | 0.04 s / 0.6 m/s2 |
| Gravity force deviation / acceleration gate | 0.35 / 0.5 m/s2 |
| Calibrated, corroborated lateral gravity gate | 0.1 m/s2 |
| Gravity innovation angle / ceiling | 0.08 / 0.12 rad |
| Gravity suppression hold | 1.2 s |
| Standstill/constraint update interval | 0.2 s |
| GPS speed noise | 0.4 + 0.04 * speed, m/s |
| GPS course noise / minimum course speed | 0.06 rad / 3 m/s |
| Compass confidence minimum / minimum interval | 0.05 / 0.1 s |
| Future GPS horizon / retained history | 2 s / 3 s |
| Mount minimum gravity windows / GPS intervals | 6 / 20 |
| Mount yaw sigma target / refinement rate | 0.07 rad / 0.002 rad/s |
| Absolute force / angular-rate bounds | 58.8 m/s2 / 8.7 rad/s |
| Robust plausibility window / soft recovery budget | 9 samples / 12 consecutive soft outliers |

## Time, corrections, and presentation

IMU measurement time drives output. GPS coordinate time dates a fix; receiver time, then envelope
source time, are fallbacks. Historical wall-clock `StaleTimestamp` annotations do not invalidate
replay. Future fixes wait for IMU time; delayed fixes use bounded snapshots and replayed corrections.
Already emitted output is never rewritten. Duplicate/regressing IMU ticks retain nondecreasing
output time with a distinct output sequence and original raw timestamp in the trace.
Received fixes and accepted correction/evidence are tracked separately. Refused velocity
measurements do not train the mounting rotation, and refused corrections do not reset GPS
freshness. Rewind snapshots retain their geodetic anchor. Sustained navigation disagreement
demotes the published calibration state and confidence rather than reporting a locked solution.

**GPS re-anchoring is quality-weighted Kalman correction, not an exact assignment to each noisy
coordinate.** This is a deliberate refinement of the original prompt's "snap": measurement
accuracy and covariance matter, and hard assignment would introduce GPS-rate jitter. Defined
synthetic checks bound anchoring error to 1.5 m and withheld-fix error to 5 m / 1 m/s.

The engine and trace use measurement time only. Gauges use one current presentation timestamp
per displayed batch: their shock decay runs against render wall time, so feeding historical
recording dates to that decay would instantly erase new shock indications. Presentation damping
does not affect the estimator or trace.

## Reproduction

From the repository root, build explicitly before exporting. The console and manifest identify
the actual loaded engine assembly, version, write time, and SHA-256 so stale binaries are visible.
Use new output paths: the tools never overwrite recordings or existing output directories.
The commands below assume an optional, Git-ignored local copy of `ReferenceData`
from the private WARP checkout; real recordings are not part of this public repository.

```powershell
dotnet build Sensored.slnx -c Debug -m:1

dotnet run --project src\Sensored\WarpSensorsDataProcessor -c Debug --no-build -- `
  analyze --recording-folder src\Sensored\ReferenceData `
  --output C:\WARP-Reference\analysis.json

dotnet run --project src\Sensored\WarpSensorsDataProcessor -c Debug --no-build -- `
  fusion-reference --recording-folder src\Sensored\ReferenceData `
  --output C:\WARP-Reference\baseline

dotnet run --project src\Sensored\WarpSensorsDataProcessor -c Debug --no-build -- `
  fusion-reference --recording-folder src\Sensored\ReferenceData `
  --scenario src\Sensored\FusionReference\reference-faults.json `
  --output C:\WARP-Reference\faults
```

The baseline uses all 25 original CSV parts, without damping, trimming, or merging away
observations. The scenario records explicit half-open fault windows and writes a replay-compatible
transformed copy separately. Its withheld GPS sidecar is evaluation data, never engine input.

Tests use the existing xUnit runner. Run one framework at a time to avoid overlapping
multi-target reference-assembly builds:

```powershell
dotnet test ..\WARP\src\Tests.UnitTests\cs\WarpToolkit.Sensors.Fusion.Tests -c Debug -f net10.0 -m:1
dotnet test ..\WARP\src\Tests.UnitTests\cs\WarpToolkit.Sensors.Fusion.Tests -c Debug -f net11.0 -m:1
dotnet test src\Sensored\WarpSensorsDataProcessor.Tests -c Debug -f net11.0 -m:1
```

## Recorded verification

Final artifacts from this implementation run are stored outside source control under the
following historical location. The rename does not move these existing files.

```text
%LocalAppData%\WARP\WarpSensorsDemo\FusionReference\golden-51c5d240
```

`baseline` contains the golden trace and provenance; `faults` contains the fault trace,
scenario, withheld fixes, and transformed recording. `verification.json` records the independent
cross-checks. Input-part SHA-256 hashes were rechecked after processing; all originals were unchanged.
The `engine` directory retains the exact implementation and abstraction assemblies matching the
manifest's engine hash, independently of subsequent rebuilds in the repository.
The artifact directory also includes `verify-reference.py` (Python standard library only):
run it there to repeat hash, coverage, seeded-fault, dropout and unchanged-input checks, or supply
`--recording-folder` when the source recording has moved.

| Baseline check | Measured result |
| --- | --- |
| IMU/output coverage | 569,712 / 569,712 |
| First calibrated | 61.994 s, within the 120 s target |
| Final state / calibrated coverage | Calibrated / 95.702% |
| Output rate | 396.534 Hz by measurement time |
| Nonfinite output values | 0 |
| Rejected ticks | 6: one pre-history GPS fix, five GPS velocity gates |
| Mean confidence | 0.822 |
| Fused speed range | 0 to 35.981 m/s |
| Pitch / roll range | -7.734 to 11.842 deg / -10.555 to 3.506 deg |
| Speed difference at newly exposed GPS fixes | 0.496 m/s RMS |
| Repeat export | Byte-identical trace hash |

Baseline trace SHA-256:
`98290a7f11a7937f216ecb0ef395ba2194cca9dac054f71b110d8bbf9a6671e5`.

Fault trace SHA-256:
`4bcb93ed5f143ea24a20d83c66e0d1875a437fdee1b0a22071e863ce6d5abb17`.

| Scenario | Measured result |
| --- | --- |
| Z +20 g, 150.000-150.050 s | All 20 ticks rejected and attitude/acceleration/speed/position held |
| Compass +90 deg, 200-205 s | 50 interference decisions; no invented vector measurements |
| GPS dropout, 300-310 s | Last withheld fix: 12.366 m / 0.980 m/s error; confidence fell 0.433 |
| Tunnel, 600-615 s | Last withheld fix: 29.346 m / 2.155 m/s error; confidence fell 0.589 |
| One withheld fix, 900-901 s | 8.305 m / 2.071 m/s error against that raw GPS fix |

The single real-drive withheld fix does **not** meet the later scoring prompt's illustrative
5 m / 1 m/s tolerances. Those tolerances are not a universal guarantee; this observed limitation
is retained rather than hidden or "fixed" by feeding the withheld point back into the estimator.
Synthetic trajectories with independently known motion pass the specified 5 m / 1 m/s checks.

The deterministic suites also cover known grades, hard braking, near-vertical and perturbed mounts,
missing sensors, timestamp faults, gyro/acceleration spikes, genuine bumps, heading wrap, delayed/
future GPS, covariance positivity, replay parity, output cadence, responsiveness, and shutdown/I/O
failure paths. The independent vertical-mount test uses scalar kinematics, not the filter's rotation
helpers; its unchanged targets are 1 deg pitch/roll RMS, 0.3 m/s2 longitudinal RMS, at most 1 deg
spurious braking pitch, and at most 150 ms half-step response.

Final execution: 99 fusion cases passed on each of .NET 10 and .NET 11, 45 processor cases passed,
122 existing raw-sensor cases passed on each framework, and the 22 abstraction / 31 presentation
cases passed on .NET 11. The Debug demo solution built without errors.

The actual WinForms form was exercised with recorded inputs, online map tiles disabled, graceful
stop, partial-artifact handling, direct unit/sign mapping checks, and historical-replay shock decay.
Full-drive numerical verification is headless; a full manual, drive-wide visual inspection is not
claimed.

## Limits and interpretation

- Mount tilt versus sustained road grade has an unobservable DC component without sufficient
  independent reference/motion evidence. Balanced synthetic grades are known truth; this real
  drive has no independently annotated attitude/grade truth.
- Accelerometer bias and gravity tilt cannot both be identified from one static gravity vector.
  Bias estimation uses other evidence and priors; calibration is not proof of absolute attitude.
- The real recording has weak compass confidence and substantial vibration. Gravity corrections
  are deliberately sparse; gyro propagation and GPS cross-checks carry intervening motion.
- Fixes outside the retained history are diagnosed rather than applied at the wrong time.
  Missing samples and implausible samples have distinct semantics.
- Traces contain sensor-rate acceleration, including real vibration/bump content, before UI
  damping. Real-drive ranges and GPS agreement are observations, not surveyed accuracy claims.
- Long outages inevitably grow uncertainty. No unbounded-outage position guarantee is made.
- Scale/misalignment and sensor lever-arm offsets are not separately estimated. Rapid stop-start
  transitions can briefly lag raw GPS speed by about 2 m/s; there is also a short calibration-state
  flicker in this drive rather than a permanently latched calibration flag.
- A completed manifest certifies transport/output completeness, not automatic numerical quality.
  Shared cutoffs, seeks, explicit Stop, and failures cannot be mislabeled as a complete golden drive.
- Determinism is verified for the pinned runtime/options; cross-platform bitwise identity is not
  promised without an actual comparison.

Model scoring and container infrastructure are separate work. Both implementation prompts now
explicitly require executable code, integration, trace generation, and executed verification.

## Replay timing and sustained-turn follow-up

The reported slow replay was reproduced independently of fusion and rendering: 5.002 seconds
of IMU measurements took 29.191 seconds to deliver. Per-sample relative delays accumulated
timer overshoot. The shared replay now schedules absolute deadlines on one monotonic clock;
the same probe took 5.010 seconds after correction. Recorded arrivals still control delivery,
while original IMU/GPS measurement timestamps continue to control numerical integration.

`replay-timing` now observes actual callbacks before service enqueue and full-rate consumption
afterward. Its versioned sidecar is separate from the locked trace CSV. The future scoring
specification distinguishes logical output cadence from real delivery pace, and deterministic
400 Hz control evidence delivered six times too slowly fails its pacing assessment.

These full-rate fusion runs each covered a 60.000-second recording window, with all
22,552 replayable rows delivered and consumed and no transport loss:

| Selected rate | Observed wall seconds | Observed rate |
| --- | --- | --- |
| 25% | 240.011 | 0.2500 |
| 50% | 120.010 | 0.5000 |
| 100% | 60.004 | 0.9999 |
| 150% | 39.999 | 1.5000 |
| 200% | 29.999 | 2.0001 |
| 300% | 20.003 | 2.9996 |

The matrix used a fixed CLI/engine snapshot to isolate pacing from ongoing numerical work.
A final-engine 300% repeat delivered the same complete window at 2.9985x, with 0.05% relative
rate error. These are local measurements, not hard-real-time guarantees.

### Roll evidence and correction

The real drive's candidate 44-74 second measurement-time window contains roll from -4.407
to +1.333 degrees, real course changes, and a transition into calibration at 61.994 seconds.
This does **not** establish whether the observed approximately four-degree bank was false.
The recording has no independent road-bank truth, and this early-window observation remains
unchanged. No fixed roll offset or forced-level correction was added.

An independent synthetic control did expose a narrower defect: after calibration, the loose
0.4-0.5 m/s2 gravity-only gates could accept the centripetal force of a gentle sustained turn.
At 20 m/s and one degree/second, approximately 0.35 m/s2 can mimic two degrees of tilt. The
old estimator attenuated known +5/-5 degree road bank, with bank RMS errors of 2.656/2.672 degrees.

`CalibratedLateralGravityAccelGate` now limits that motion to 0.1 m/s2 after mounting is known,
for both attitude gravity observations and mounting gravity windows. Where the interval has
complete gyro evidence, yaw must corroborate the GPS turn; course noise alone does not discard
the gravity reference. When low-speed GPS course is unavailable, GPS speed combined with
observed yaw is usable motion evidence if the down axis is established. Missing gyro intervals
remain unknown, never assumed to be zero. Acquisition retains its noise-tolerant gates, and
the existing independent longitudinal-residual pitch correction is unchanged.

The fixed-threshold, 420-second controls cover both turn directions, simple and near-vertical
mounts, real bank, return to level, gyro bias, noisy straight-line course, and irregular/delayed
GPS delivery. Final +5/-5 degree bank RMS errors are 0.028/0.067 degrees; the delayed,
irregular-GPS +5-degree case is 0.093 degrees. All retain the original one-degree acceptance
limits. A straight-line noisy-course control stays at 0.060 degrees roll RMS.

All 109 fusion cases pass on .NET 10 and .NET 11. Re-exporting the unmodified complete reference
drive and the existing fault scenario produces the **same baseline and fault trace hashes**
listed above, with all 569,712 IMU output rows. Their unchanged hashes are a regression result,
not proof that this real drive's absolute pitch/roll is correct. The shared optical-direction
and SVG changes are separately backportable; these estimator and grading changes are not.

## Seeking and retained installation calibration

A reported field symptom was that using a replay time control left only the speed read-out alive,
with attitude, acceleration and heading dead until the application restarted. The cause was this
branch's estimator lifecycle, not the shared replay transport: a seek built a brand-new engine, so
the device-to-vehicle mounting had to be re-learned from roughly a minute of excited driving. Until
it converged, `Validity` carried `Navigation` only, which is exactly the observed behaviour.
`MainForm.Fusion.cs` does not exist on `main` or on the pre-Golden branch, whose gauges read raw
sensor values directly, so this defect is Golden-only and must not be backported.

Seeking moves recorded time; it does not move the device inside its mount. `ResetNavigation()`
therefore clears attitude, velocity, position, biases, uncertainty, history, pending corrections
and the output sequence, while retaining the mounting rotation and magnetic alignment. `Reset()`
keeps its full meaning for an unrelated drive, device or source, and the demo still builds a new
engine when a different recording is opened or acquisition switches between replay and live.

Deterministic controls cover all three states: after `ResetNavigation()` the very first estimate is
again `Calibrated` with `Attitude` and `Acceleration` valid while navigation restarts from no
position; after `Reset()` the same seek destination reproduces the reported dead indicators for the
rest of the drive; and repeated seeks keep restoring the read-outs. Against the real recording at
300% playback, a seek during a calibrated drive previously never recovered within 90 seconds and now
recovers in 0.0 seconds with `Attitude, Heading, Acceleration, Navigation` intact.

## Pitch sign against the reference drive

A field report said the artificial horizon showed downhill while the car was climbing. The published
convention is pitch positive nose-up, and the horizon moves down (more sky) for positive pitch.
That picture was not changed.

The checked-in Redmond–Duvall recording was exported with the current engine (trace hash
`98290a7f11a7937f216ecb0ef395ba2194cca9dac054f71b110d8bbf9a6671e5`). On 15 sustained
monotonic grades of at least 6 m, published pitch agreed in sign with GPS altitude change on 13.
A regression of those segments is about `pitch = 0.88 * grade + 2.2°`. The largest climbs
(about +4° to +6°) are reported nose-up, and the later steep descents are reported nose-down.
The two sign disagreements are early or shallow descents that stay slightly nose-up, which matches
the +2° bias rather than a swapped axis.

Roll stays slightly negative on many straight segments (about -1° to -4°) and was not flipped.
A curve is not a bank, and the recording has no independent bank truth. Negating either angle
would break the synthetic +5° grade contract and the horizon geometry test without matching the
drive.
