# Golden fusion trace schema (version 2)

Locked identity: `warp-fusion-trace` version **2**.
Writer: `WarpToolkit.Sensors.Fusion.FusionTraceWriter`.
Header: `FusionTraceSchema.Header` (72 columns, semicolon-separated).

This file is the output contract. Column names, order, and encoding do not change
without incrementing the schema version. The first 62 columns retain the version 1
order and encoding; version 2 appends ten coordinate-free mounting diagnostics.
Historical version 1 traces remain 62 columns and should not be relabeled.

## File rules

- UTF-8, no BOM.
- Newlines are `\n`.
- First line is the header row (column names, never quoted).
- One data row per non-null `FusionEstimate`.
- Call `WriteAsync` for **every** input. Auxiliary observations do not emit a
  row; they are audited on the next estimate row.
- `FileMode.CreateNew`: the path must not already exist.
- `DisposeAsync` flushes. The writer is single-owner and not concurrent.
- Atomic publish, hashes, and completion flags live in the CLI manifest, not in
  this CSV.

## Field encoding

| Kind | Encoding |
| --- | --- |
| `double` | Invariant `R` round-trip. `NaN`, `Infinity`, and `-Infinity` are written explicitly. Blank means the field does not apply. |
| `long` | Invariant integer. Blank when unknown (live demo has no recording row). |
| `bool` | `true` / `false`. |
| timestamp | UTC round-trip ISO-8601 (`yyyy-MM-ddTHH:mm:ss.fffffffZ`). Default timestamps are blank. |
| enum / flags | `ToString()` (`None`, `Imu`, `Attitude, Heading`, `Calibrated`, …). |
| text | Raw text. A field is CSV-quoted only when it contains `;`, `"`, CR, or LF. Embedded quotes are doubled. |

Culture must not affect output. Variable run metadata must not appear in this file.

## Columns

Order is contractual.

| Column | Source |
| --- | --- |
| `out_seq` | `FusionEstimate.Sequence` |
| `out_t` | `FusionEstimate.Timestamp` (IMU measurement time; nondecreasing) |
| `dt_s` | Seconds since previous estimate timestamp; blank on the first row; `0` when the timestamp did not advance |
| `elapsed_s` | Seconds from the first observed IMU source timestamp to this row's IMU source time |
| `rec_row` | Original recording row of the triggering input; blank when not file-driven |
| `in_seq` | Triggering `SensorEnvelope.Sequence` |
| `in_kind` | Triggering `SensorDataKind` |
| `in_t_source` | Triggering envelope source timestamp |
| `in_t_arrival` | Triggering envelope arrival timestamp (provenance only) |
| `in_validity` | Triggering `SensorSampleValidity` |
| `imu_*` | Raw IMU payload of the triggering input; blank when the trigger is not IMU. `NaN` magnetic/inclinometer values are preserved. |
| `gps_*` | Latest GPS whose **measurement time** is ≤ this row's IMU source time (see below). Blank until such a fix exists. |
| `pitch_deg` … `speed_mps` | Estimate attitude, gravity-compensated vehicle acceleration (m/s²), speed (m/s) |
| `pos_lat` / `pos_lon` | Estimate WGS84 degrees; blank when `Position` is null |
| `calib_state` | `Uncalibrated` / `Converging` / `Calibrated` |
| `confidence` | `[0, 1]` |
| `rejected` | `FusionEstimate.RejectedSample` |
| `out_validity` | `FusionOutputValidity` flags |
| `diag_*` | `FusionDiagnostics` |
| `aux` | Compact audit of every non-emitting input since the previous output row |
| `diag_down_spread_rad` | Angular spread of accepted device-gravity windows |
| `diag_yaw_sigma_rad` | One-sigma mounting-yaw uncertainty |
| `diag_yaw_stability_rad` | Spread of recent yaw fits |
| `diag_mount_excitation` | Accumulated in-plane motion evidence |
| `diag_mount_gain` | Fitted motion gain; must satisfy the physical consistency gate |
| `diag_gravity_windows` / `diag_gps_intervals` | Accepted gravity windows and retained GPS motion intervals |
| `diag_has_forward_axis` | Whether vehicle forward is established; independent of full calibration |
| `diag_mount_gate` | Reason the mounting is not fully calibrated, or a navigation-divergence demotion |
| `diag_calib_transition` | State change on this output row (`previous->current`); otherwise blank |

The ten new fields follow `aux` (column 62) in exactly the order above.
Direct writer callers without a captured mount snapshot leave the mount-evidence
columns blank; a calibration transition can still be emitted. None of these fields
contains position, raw NMEA sentences, or other location data.

Rejected IMU ticks still produce a row (held estimate, original raw values, original
input timestamp in `in_t_source`). Duplicate/regressing IMU ticks keep the last
output timestamp in `out_t`.

## GPS provenance and future leakage

GPS measurement time, matching `IFusionEngine` / `GpsObservation.TryCreate`, in order:

1. `GeolocationPayload.CoordinateTimestamp` when it is not default
2. otherwise receiver `GeolocationPayload.SourceTimestamp` when it is not default
3. otherwise the envelope `SourceTimestamp`

`gps_t_source`, `gps_t_coord`, and `gps_t_fix` always preserve the original three
clocks. `gps_meas_t` is the selected measurement time. `gps_age_s` is
`out_t` (`FusionEstimate.Timestamp`) minus `gps_meas_t`. On a rejected/regressing
IMU the estimate retains the previous output timestamp; age and GPS eligibility
follow that held navigation time, not a regressing `in_t_source`, so `gps_age_s`
is not a misleading negative.

A fix timestamped **after** this row's `out_t`:

- may appear in this row's `aux` if it arrived in stream order before the tick
- must **not** appear in `gps_*` columns until a later output time catches up

Default or missing GPS measurement times stay in `aux` only and never enter the
pending-future GPS queue. Exceeding 64 pending future fixes or 1024 auxiliary
events between ticks **fails the writer**; a complete golden trace never drops
that provenance.

Multiple GPS (or other auxiliary) observations between ticks are all listed in
`aux`. `gps_*` columns show only the latest eligible fix (later measurement time
wins; equal times keep the later stream observation). Earlier rows are never
rewritten.

## `aux` compact field

Events are joined with `|`. Fields inside an event are joined with `,`.
Text inside an event escapes `\`, `|`, and `,` as `\\`, `\|`, `\,`.

| Prefix | Kind |
| --- | --- |
| `I` | IMU that did not produce an estimate |
| `G` | Geolocation fed to the writer |
| `H` | Health |
| `X` | Gap |
| `C` | Classification |
| `U` | Unknown kind |

`G` payload: `G,row,seq,t_source,t_arrival,validity,lat,lon,alt,hacc,vacc,speed,speedValid,course,courseValid,posSrc,t_coord,t_fix,conf`

Auxiliary observations after the last estimate have no row to attach to; they are
not in the CSV. Scenario-withheld GPS are **not** writer inputs; they belong in
the reference sidecar, not `aux`.

The writer does not retain the full drive. Only auxiliary events since the last
output row and still-pending future GPS with usable timestamps are buffered.
Buffer overflow is a hard failure, not an `O` token.

## Engine and time

The exporter feeds `IFusionEngine.Process` in recorded order using measurement
timestamps. There is no wall-clock pacing and no service queue. A complete export
requires nonempty IMU data, exactly one estimate per IMU row (including
rejected/held ticks), and null estimates for every auxiliary row. Violations
mark the artifact incomplete. IMU-driven output count must equal IMU input count
(ReferenceData: 569712). The manifest records `engineAssemblyVersion`,
`engineAssemblyFileVersion`, `engineAssemblyPath`, `engineAssemblyLastWriteUtc`,
and `engineAssemblySha256` so a stale or dirty engine binary is obvious even when
`FusionOptions` match. The console summary prints the resolved assembly path and
write time on every `fusion-reference` run (including `dotnet run --no-build`).

## CLI artifacts

`fusion-reference --output <new-dir>` publishes atomically:

- `golden.trace.csv` — this schema
- `manifest.json` — `warp-fusion-reference-manifest` version 1 (hashes, runtime,
  counts, calibration, cadence, completion). Variable run metadata lives here,
  not in the trace.
- `scenario.json` — copy of the input scenario when `--scenario` is supplied
- `withheld-gps.csv` — GPS withheld from the engine for evaluation
- `transformed/part-0000--merged.csv` — optional replay-compatible copy

Completion is `Complete` only after a successful atomic directory move. Failures
and cancellation write `<output>.incomplete` with `completion: Incomplete`.

## Scenario JSON (`warp-fusion-scenario` version 1)

Locked for persistent fault JSON files. CamelCase names. Enums are PascalCase
strings (not integers). Extra properties are ignored. Unknown `type` fails.

Origin is the **first recording envelope** `SourceTimestamp`. Windows are
half-open `[startSeconds, startSeconds + durationSeconds)` on that timeline.
Faults are applied **outside** the engine, in array order. Source recordings
are never modified.

### Root

| Field | Required | Default | Meaning |
| --- | --- | --- | --- |
| `schema` | yes | — | Must be `"warp-fusion-scenario"` |
| `version` | yes | — | Must be `1` |
| `name` | no | `"baseline"` | Label copied into the manifest |
| `exportTransformedCopy` | no | `false` | Write replay-compatible `transformed/part-0000--merged.csv` |
| `faults` | yes | — | Array (may be empty). Null is rejected |

### `faults[]`

| Field | Required | Meaning |
| --- | --- | --- |
| `type` | yes | `ImuSpike` \| `CompassDisturbance` \| `GpsDropout` \| `GpsTunnel` \| `GpsWithheldFixes` |
| `startSeconds` | yes | Finite, ≥ 0 |
| `durationSeconds` | yes | Finite, > 0 |
| `axis` | `ImuSpike` only | `"X"` \| `"Y"` \| `"Z"` (case-insensitive) |
| `delta` | `ImuSpike` only | Finite; added to raw accelerometer **g** on `axis` |
| `headingDeltaDegrees` | `CompassDisturbance` only | Finite; added then wrapped into `[0, 360)` |
| `confidence` | no | If finite, replaces `MagneticHeadingConfidence` during a compass disturbance |

`CompassDisturbance` does not invent a heading when the raw value is NaN.
`GpsDropout`, `GpsTunnel`, and `GpsWithheldFixes` all withhold GPS from the
engine and writer; withheld fixes go to `withheld-gps.csv` for evaluation.

```json
{
  "schema": "warp-fusion-scenario",
  "version": 1,
  "name": "example",
  "exportTransformedCopy": false,
  "faults": [
    { "type": "ImuSpike", "startSeconds": 120, "durationSeconds": 0.05, "axis": "Z", "delta": 20 },
    { "type": "CompassDisturbance", "startSeconds": 180, "durationSeconds": 5, "headingDeltaDegrees": 90, "confidence": 0.2 },
    { "type": "GpsDropout", "startSeconds": 300, "durationSeconds": 10 },
    { "type": "GpsTunnel", "startSeconds": 400, "durationSeconds": 15 },
    { "type": "GpsWithheldFixes", "startSeconds": 500, "durationSeconds": 8 }
  ]
}
```
