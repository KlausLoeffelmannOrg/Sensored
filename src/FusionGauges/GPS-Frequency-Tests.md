# COM8 GPS frequency measurements

Measured on 2026-10-16 with the connected receiver, using the Release libraries.
No coordinates or unique device identifiers were retained in this report.

## Receiver and safety

- Port COM8, opened by the source at 9600 baud.
- Read-only MON-VER: firmware `ROM CORE 3.01 (107888)`, hardware `00080000`,
  UBX protocol `18.00`; no module-name extension was reported.
- Initial readback: 1000 ms measurement interval; GPS + BeiDou + QZSS enabled.
- Non-passive cases were explicitly authorized for this test. Only volatile
  CFG-GNSS and CFG-RATE settings were changed. No CFG-CFG/nonvolatile save, baud
  change, receiver reset, or sentence-output reconfiguration was performed.
- Every case stopped and verified restoration of the saved original configuration.
  Some ACKs were not observed; exact payload readback verified restoration instead.
  The receiver was not left in a high-rate profile.

## Method

First identify the already-open receiver using
`SerialNmeaGpsSource.ReadReceiverCapabilitiesAsync`. For each profile/target, use
`SerialNmeaGpsSourceOptions.TargetFixIntervalMilliseconds` (null means Dynamic),
wait for bounded configuration completion, allow a two-second settling window,
then count accepted `FixReady` callbacks for ten seconds.

Count distinct `PositionSourceTimestamp` values separately. Measurement Hz is
`(distinct count - 1) / (last measurement - first measurement)`; arrival Hz is
callbacks / actual elapsed time. Thus short-window edge effects and missing fixes
are visible rather than conflating a configured rate with a measured rate.
The source's smoothed arrival-rate diagnostic is a third, different quantity.

Each source was stopped in a `finally` block. A configuration failure stopped the
batch; the remaining GLONASS case was run separately only after Galileo's restoration
had been verified. No user-owned process was stopped to obtain COM8.

These short tests do not establish long-term reliability, cold-start acquisition
time, moving-vehicle accuracy, or behavior under different sky/antenna conditions.

## Results

All callback counts below were also the distinct valid measurement counts.

| Profile | Target | Receiver interval | Fixes / 10 s | Arrival Hz | Measurement Hz |
|---|---|---|---:|---:|---:|
| Passive | Unchanged | 1000 ms | 10 | 1.000 | 1.000 |
| GPS + QZSS | 1000 ms | 1000 ms | 10 | 1.000 | 1.000 |
| GPS + QZSS | 500 ms | 500 ms | 19 | 1.898 | 2.000 |
| GPS + QZSS | 200 ms | 200 ms | 49 | 4.894 | 4.898 |
| GPS + QZSS | 100 ms | 100 ms | 95 | 9.499 | 9.495 |
| GPS + QZSS | Dynamic | 100 ms requested/verified during configuration | 100 | 9.994 | 10.000 |
| GPS + BeiDou + QZSS | 1000 ms | 1000 ms | 10 | 1.000 | 1.000 |
| GPS + BeiDou + QZSS | 500 ms | 500 ms | 20 | 1.998 | 2.000 |
| GPS + BeiDou + QZSS | 200 ms | 200 ms | 48 | 4.798 | 4.796 |
| GPS + BeiDou + QZSS | 100 ms | 100 ms | 100 | 9.999 | 10.000 |
| GPS + BeiDou + QZSS | Dynamic, previous candidate order | 200 ms | 50 | 4.998 | 5.000 |
| BeiDou only | Dynamic | 100 ms requested/verified during configuration | 0 | 0 | Unavailable |
| Galileo only | Dynamic | Configuration rejected; restored 1000 ms | 9 baseline-profile fixes | 0.900 | 1.000 baseline only |
| GLONASS only | Dynamic | 100 ms | 0 | 0 | Unavailable |

BeiDou and GLONASS reported no usable fix during their short measurement windows
(GGA quality 0 / receiver-no-fix). This does **not** prove that their maximum update
rate is zero or that their constellations are unsupported. A longer acquisition
window with suitable reception is needed before comparing valid-fix performance.

Galileo's CFG-GNSS readback differed from the requested configuration. The source
reported configuration-unavailable and restored the exact original settings; the
subsequent baseline fixes are **not** Galileo-rate evidence. A CFG-GNSS capability
block alone was not sufficient to prove that this firmware would enable Galileo.

Read-only identity polling timed out in the GPS-Dynamic and BeiDou-Dynamic cases
after high-rate configuration, even though the earlier configuration transaction
had identified and verified the receiver. Those later capability results remain
Unknown; they were not substituted with invented facts.

## Verification through both applications

The final live test used the actual FusionGauges `SensorHub` and Sensored
`GpsMotionSession` against one shared service and COM8 source. No UI timer was
running, and no IMU hardware or recording was started. Sharing the service avoids
trying to open the exclusive serial port in two processes.

With GPS + BeiDou + QZSS and the updated Dynamic negotiation:

| Measurement | Observed |
|---|---:|
| Requested interval | 100 ms / 10 Hz |
| FusionGauges full-rate raw history additions in 10 s | 93 |
| Sensored GPS-motion processed envelopes in the same 10 s | 93 |
| FusionGauges rolling two-second arrival rate | 9.421 Hz |
| Sensored latest measurement interval | 100 ms |
| Source smoothed arrival-rate diagnostic | 9.708 Hz |
| Subscription drops / ingress gaps | 0 / 0 |
| Original volatile settings restored on shutdown | Verified |

This verifies the real application processing owners, not just a fake source or
the serial callback. It is not a claim that two complete GUIs simultaneously owned
COM8 or that every delivered fix is rendered as an individual UI frame.

## Findings and implementation

There was no one-second cap in the NMEA parser or shared sensor pipeline.
Fractional RMC timestamps survive parsing, callbacks are enqueued without rate
limiting, and the samples' full-rate GPS consumers are independent of repainting.
Passive mode intentionally retains this receiver's original one-second setting.
Windows Location's report interval is a separate provider request and does not
configure serial hardware.

The previous multi-GNSS Dynamic negotiation started at 200 ms based on an assumed
5 Hz ceiling. The explicit 100 ms test disproved that ceiling for this receiver.
Dynamic now tries 100, 200, 250, 500 and 1000 ms for every selected profile, accepting
only ACK/readback-verified configurations. Explicit targets try only the requested
interval; failure is visible and restores the original configuration, not a hidden
fallback. Measured valid-fix frequency remains separate from configuration.

In FusionGauges, select **GPS-Configuration > Serial**, choose the desired real
GNSS profile, choose COM8 and 100 ms or Dynamic, and explicitly authorize temporary
receiver commands for this app session. Apply by the next explicit Stop/Start
while parked. A saved profile alone never authorizes receiver writes.
