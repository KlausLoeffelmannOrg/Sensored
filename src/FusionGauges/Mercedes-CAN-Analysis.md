# Mercedes W166 capture: conclusion and next steps

Research checked 2026-10-25. This concerns diagnostic recording, not vehicle control.

## What the supplied capture establishes

**Keep the current adapter for now: filtered passive reception works. Missing gear
and torque are not evidence of a Bluetooth limitation.**

All setup/filter commands returned `OK`; `STM` returned **199 eight-byte frames**
and **567 candidate decodes**. Reception ran from 18:12:11.100 to 18:12:13.795,
about 2.7 seconds, not the advertised *maximum* 30-second budget. `STOPPED`
terminated monitoring; the transcript reports successful OBD restoration and polling
resumption at 18:12:14.022. It does not include subsequent PID replies proving cadence.

| CAN ID | Frames | Candidate observations |
|---|---:|---|
| `003` | 76 | Steering -4 degrees, steering rate 0 |
| `105` | 39 | Engine 854-873 rpm, pedal 0%, kickdown off |
| `203` | 38 | All four wheel speeds 0 rpm |
| `245` | 38 | Yaw rate 0.02-0.17 degrees/s; longitudinal -0.24 to -0.16 m/s2; lateral 0.08 m/s2 |
| `30D` | 8 | Coolant 32 C, intake 9 C, oil 15 C; candidate fuel consumption 305.74-312.24 microlitres/250 ms |

These look plausible for a stationary, idling vehicle, but plausibility is not
validation. The decoder explicitly uses **W204 `204_POWERTRAIN_SB` definitions**,
not a verified W166/hybrid database [1]. Checksums, validity values, scaling and
hybrid torque reference points still need confirmation.

The other seven requested IDs (`14B`, `0F3`, `005`, `0F1`, `1CD`, `2E2`, `2F1`)
never appeared. In particular, there are **no captured gear or engine-torque
frames**. This was a short, twelve-ID-filtered observation, not an inventory of
the entire bus. Different IDs, operating conditions, ECU variants or gateway
routing remain possible explanations. No battery, fuel-level or actual/commanded
throttle values can be established from this passive excerpt; pedal position is
not throttle position.

The battery indicator uses the existing independently timestamped Mode 01 PID
`5B` measurement. Its generic label is "hybrid battery pack remaining life" [2];
the reported correlation with charge on this vehicle justifies the UI's
**Battery** label, not a universal reinterpretation or a battery-power estimate.

## Practical acquisition options

| Approach | What it improves | What it does not solve |
|---|---|---|
| **Existing OBDLink CX** | Already receives the selected CAN frames; lowest-cost next experiment | Unknown W166 IDs/scaling, inaccessible networks, or missing ECU definitions |
| **OBDLink MX+ Bluetooth** | Alternative supported radio/interface where a particular application needs it | Advertised extra networks are Ford MS-CAN and GM SW-CAN, not guaranteed Mercedes gear/hybrid coverage [3] |
| **OBDLink EX USB** | Wired host link; useful to compare disconnects, latency and throughput | Primarily optimized for FORScan/Ford; USB alone does not reveal hidden Mercedes signals [4]. SX is now discontinued [5] |
| **Isolated PCAN-USB / Kvaser Leaf v3** | Dedicated raw-CAN logging, vendor Windows APIs; Leaf v3 specifies silent mode, isolation, 50-us timestamps and 20,000 messages/s [6][7] | Still only sees the physically connected network. Requires an appropriate cable, verified wiring and new application transport integration |
| **Licensed Mercedes XENTRY + suitable interface** | Best reference for exact ECU identity and available engine/transmission/hybrid live-data channels | Not a ready-made FusionGauges streaming API. Model, market, account eligibility, licensing and interface compatibility must be checked [8] |

Mercedes advertises XENTRY Pass Thru EU for Euro 5/6 model series and BEVs with
technical interface restrictions [8]. Verify the exact vehicle and locally
available XENTRY product with Mercedes before buying a J2534 interface; do not
assume any J2534 device supplies full coverage. No purchase is guaranteed by this
research to deliver the requested W166 signals.

**Software integration matters:** today's session requires `IBluetoothTransport`
and explicitly checks for `OBDLink CX`. EX/MX+/PCAN/Kvaser are not drop-in choices
in the current app, even when their hardware can communicate with the vehicle.

## Recommended order

1. Keep standard OBD recording. Confirm support bitmaps and the ECU supplying
   `11` (actual throttle), `4C` (commanded throttle), `2F` (fuel) and `5B`
   (battery). Check advertised `61`/`62`/`63` torque support before pursuing
   proprietary torque: actual percent torque plus reference torque can provide
   **ECU-reported engine torque**, not total hybrid/wheel torque [2].
2. Obtain an authorized XENTRY live-data reference for the exact engine,
   transmission and hybrid ECUs. Establish actual/target gear, torque meaning,
   battery charge and battery power separately.
3. For app integration, use documented ECU addresses and read-data definitions.
   UDS `22`/`62` with ISO-TP is a possible readout path, but addresses, DIDs,
   byte layout, units and permissions are vehicle-specific, not supplied by a
   faster adapter [9]. Do not brute-force DIDs or add coding/security-unlock
   operations.
4. If raw logging is still needed, choose an **isolated USB-CAN interface** over
   another Bluetooth dongle. Compare a broader, bounded capture against the
   reference. Passive mode must not silently replace live speed: this app pauses
   standard OBD while monitoring. Retain raw bytes and explicit observation gaps.

Configure tests while parked; use a passenger or unattended logger for road
recording. Any internal-network connection needs qualified, vehicle-specific
wiring verification. Do not bypass a gateway or probe high-voltage circuitry.

## Sources

1. [Public W204 candidate source](https://github.com/rnd-ash/ultimate-nag52-fw/blob/main/lib/egs53_ecus/can_data.txt) and repository `MercedesEgs53Module.cs`; not W166 certification.
2. [CSS Electronics: standard OBD PID table](https://www.csselectronics.com/pages/obd2-pid-table-on-board-diagnostics-j1979).
3. [OBDLink MX+ manufacturer description](https://www.obdlink.com/products/obdlink-mxp/).
4. [OBDLink EX manufacturer description](https://www.obdlink.com/products/obdlink-ex/).
5. [OBDLink SX discontinuation notice](https://www.obdlink.com/products/obdlink-sx/).
6. [PEAK PCAN-USB product/API information](https://www.peak-system.com/products/hardware/external-pc-interfaces/pcan-usb/). Isolation is an option: choose the isolated version.
7. [Kvaser Leaf v3 specifications](https://kvaser.com/product/leaf-v3/) and [silent-mode definition](https://kvaser.com/knowledge-base/what-is-silent-mode/).
8. [Mercedes XENTRY Pass Thru EU](https://b2bconnect.mercedes-benz.com/gb/shop/workshop-solutions/xentry-pass-thru-eu) and [licensing/compatibility FAQ](https://b2bconnect.mercedes-benz.com/gb/help/faq/software/xentry-pass-thru-eu).
9. [CSS Electronics: UDS, ISO-TP and read-data services](https://www.csselectronics.com/pages/uds-protocol-tutorial-unified-diagnostic-services).
