namespace Sensored;

/// <summary>
///  Provides offline, location-free help documents for the sensor dashboard.
/// </summary>
internal static class HelpContent
{
    internal static string GetTitle(HelpTopic topic)
        => topic switch
        {
            HelpTopic.Gps => "GPS and receiver help",
            HelpTopic.Map => "Map and privacy help",
            HelpTopic.Status => "Dashboard status help",
            _ => throw new ArgumentOutOfRangeException(nameof(topic))
        };

    internal static string GetMarkdown(HelpTopic topic)
        => topic switch
        {
            HelpTopic.Gps => GpsMarkdown,
            HelpTopic.Map => MapMarkdown,
            HelpTopic.Status => StatusMarkdown,
            _ => throw new ArgumentOutOfRangeException(nameof(topic))
        };

    private const string GpsMarkdown =
        """
        # GPS and receiver help

        ## TL;DR

        Select a GPS source from **Sources → GPS** or **Options → Source**, then **Start** (or **Restart** while parked if a session is running). A selected profile is a request, not proof that the receiver applied it or that a satellite contributed to a fix. A connected receiver can still report **NO FIX**. Check accepted fix age and the measured rate before trusting a position.

        ## Source, satellites, and constellations

        - **Windows Location** reports positions through the operating system; it need not expose satellite or receiver details.
        - **Serial NMEA** reads messages from a receiver. A passive profile listens without changing the receiver. Active profiles need per-session authorization; changing GNSS or rate settings can be temporary, and restoring the previous receiver settings must be verified.
        - **GNSS** means global navigation satellite systems. A constellation is a satellite system (such as GPS, Galileo, GLONASS, or BeiDou). **Configured constellations** are receiver settings confirmed by readback; **contributing constellations** require live satellite-use evidence from GSA messages. Neither a profile selection nor readback alone proves satellites are contributing.
        - **GGA quality 0** means the receiver reported no usable fix. Clear sky and antenna reception matter; changing a profile cannot create a satellite signal.

        ## Requested rate versus measured rate

        **Hz** means updates per second. The requested rate is a configuration target; it is displayed as applied only after receiver readback. The measured rate counts accepted fixes actually arriving now. A source-provided rate estimate may describe older observations. Link traffic, rejected fixes, and receiver limitations can make measured rate lower than requested. Replay rate is playback cadence, **not** a live GPS fix rate.

        The Windows Location report interval is a reporting request, not a promise of fresh positions at that rate. The observed accepted Hz counts valid fixes actually arriving and may be lower than the request.

        ## Fix source and quality

        **Link/transport Ready** means the source is connected, not that a valid fix exists. Windows Location and serial NMEA can have different levels of detail. An **accepted fix** passed validation; **rejected fixes** were counted but not used for the live route. The last rejection reason helps explain missing updates. Fix age is measured from the last accepted arrival where available; **fresh** means within the adaptive interval, **STALE** means older, and **NO FIX** means no accepted fix in the current session. A missing age or satellite count is *unknown*, not zero and not healthy. Recorded arrival timestamps do not establish live fix age.

        ## Configure, Start, and Stop

        **Fusion setup** prefills a new mounting reference from the current tablet pose under your explicit assumption that the vehicle is level and parked. Verify the selected device direction pointing forward: gravity determines down, not vehicle forward. An existing reference is preserved until you choose **Re-capture current sensor pose**; independently measured axes can also be entered manually. Mount changes apply to the next live session and do not interrupt acquisition or recording. Legacy **Options → Calibration** preferences do not configure the current fusion engine.

        **Sources** on the toolbar contains GPS, OBD2, and Sensors. **Options → Source** selects the GPS provider, COM port and receiver profile. **Options → Bluetooth** discovers paired and unpaired devices and remembers explicit OBD2 and WT901 sensor choices. A 20-second scan shows progress, device count and errors in the status bar; use Stop scan or Scan again as needed. Automatic retains paired-compatible discovery. Explicit devices connect without requesting pairing, but Windows consent or device security may still require it. The OBD protocol and sensor endpoints remain validated. Device/protocol changes reconnect enabled live sources without restarting the drive; replay stays isolated. **Options → Sensors** selects the display reference and opens the live pose viewer for independent internal and Bluetooth yaw/pitch/roll offsets. GPS changes apply to live acquisition without restarting the drive. Display references never alter the fusion measured mount. Missing, stale or moving GPS data may prompt a warning before receiver commands or restoration. Choose **Yes** to override incorrect motion detection for that operation, or **No** to cancel. **Start** begins acquisition; **Restart** releases the previous session; **Stop** ends it. Authorized active profiles may change volatile receiver GNSS/rate settings and must verify restoration later. Operate the UI only when safe.

        Raw-sensor diagnostics and GPS comparison belong to the separate **WarpSensorsDemo** diagnostic app. Sensored retains the driving dashboard, recording timeline, replay, map, and this local help.

        ## Opt-in road learning Quizz

        After starting live acquisition, choose **Start learning Quizz** when safe. Only a passenger may interact while moving. The large window guesses a changed motion condition using a rolling 30-second context. Choose **Level 1..5**, or **0 - Wrong assessment** to reject that condition. Silence and dismissal never count as answers.

        **Disregard Last** retracts your previous answer. The arrows select a pause of **1..20 minutes**; **Pause** dismisses the current question without stopping sensing. **End Quizz** ends learning. Questions expire after ten seconds. Tapping the quiz StatusStrip opens a larger source-evidence table and discards the question as stale.

        Confidence is an experimental rule score, not calibrated accuracy. Motion alone cannot establish road grade, a stop sign versus a traffic light, reverse, parking intent or highway entry/exit. These conditions remain unguessed without independent evidence. Answers and frozen sensor examples are saved locally under **WARP\Sensored\RoadLearning\quiz-v1** in LocalAppData; undo preserves an explicit retraction. This is data collection, not online training. Quiz labels never change fusion calibration or physical support. **Fusion setup** and Tools/Options calibration remain available.

        ## Map privacy

        Route and marker can be drawn without online tiles. Online OpenStreetMap tiles are opt-in; requesting them discloses the viewed area to the tile provider. The help window itself is entirely local and never requests a map, a link, or your location.
        """;

    private const string MapMarkdown =
        """
        # Map and privacy help

        ## TL;DR

        Route and current marker remain visible without a tile background. Online OpenStreetMap (OSM) tiles are **off unless you allow them**; requesting tiles sends the viewed area to the tile provider. The help window does not load maps or send location data.

        ## Online and offline

        **Online** means tile downloads are allowed and available; **Loading** means a request is in progress; **Unavailable** means a tile request failed; **Offline** means online tiles are not enabled. A failed request does not remove loaded tiles or cancel the remaining downloads. The route and marker do not depend on tile availability. **Retry online tiles** retries failed downloads without toggling permission. It only requests tiles when network use is allowed. Windows downloaded offline maps are not a tile source for this control.

        ## Layout and zoom

        Map enabled controls whether the dashboard map is shown. Row and column span change its share of the dashboard. Zoom adjusts the view relative to the automatically fitted route. No map setting changes the underlying sensor fix quality.

        Status changes roll a translucent notification over the map, remain for ten seconds, then fade away. Double-click or double-tap the map to recall the last notification; tap the notification to dismiss it. Long notifications can be scrolled with the mouse wheel. **Fusion setup** still provides the full support details.

        ## Consent and help

        Select **Allow ONLINE OpenStreetMap tiles (shares viewed area)** to opt in; there is no additional confirmation dialog. This choice is saved immediately when changed in the map menu. The viewed area, not the contents of this help document, is sent to the tile provider. Disable online tiles whenever you prefer a route-only view.
        """;

    private const string StatusMarkdown =
        """
        # Dashboard status help

        ## TL;DR

        **Sensors Hz** and **GPS Hz** are observed update rates, not promised acquisition rates. **Ready** describes the app or link state, not proof of a valid GPS fix. GPS tooltips retain **NO FIX**, **STALE**, and receiver details; recording and receiver restore warnings remain visible. The ? button always reuses this help window.

        ## Status strip glossary

        - **Sensors Hz / IMU**: measured inertial measurement unit update rate (acceleration, rotation, and related sensors). **GPS Hz**: measured accepted position update rate. Hz is updates per second.
        - **Sources**: one toolbar menu for GPS, OBD2, and display sensors. Under GPS, Windows Location and Serial NMEA receiver are leaves; COM port, GPS type, and target rate are siblings of those leaves.
        - **GPS source**: under **Sources → GPS**. **Windows Location** and **Serial NMEA receiver** are selectable leaves. **COM port**, **GPS type**, and **Target GPS rate** are siblings of those leaves, not nested under the receiver. Target GPS rate requests Auto, 1, 2, 5, or 10 Hz for the next Start/Restart. A request is not a verified receiver rate.
        - **Option checkmarks**: the toolbar's right-side gutter shows selected options and submenu arrows without covering their symbols.
        - **OBD2**: under **Sources**. Shows the selected device and measured speed-report Hz. Library-owned discovery starts immediately when enabled, independently of IMU/GPS, and lasts 10 minutes by default (Options → Bluetooth: 1–15 minutes); a late connection switches speed input even while recording.
        - **Sensors**: under **Sources**. Internal is the default display reference. Bluetooth stays disabled until a fresh reading is available, and Internal is disabled during replay. Options → Bluetooth selects the sensor device and sets a 1–15 minute connection/search window (3 by default). Options → Sensors edits independent yaw/pitch/roll references. Edit opens the live tablet pose viewer: capture the current pose as zero, enter offsets manually, or reset to all zero. Those references never change the fusion measured mount. Raw gravity is apparent tilt affected by vehicle acceleration. Bluetooth yaw is device-fused and is not a certified independent compass.
        - **Spherical horizon**: Options → Display, on by default. Heading is drawn on the horizon sphere, the separate compass is hidden, and the map uses the full panel width unless the map itself spans the gauge area.
        - **Keep display awake**: Options → Display, on by default. Disables the screensaver without taking ownership of full screen.
        - **Last exception**: click the red message to open its details. Click again, click the popup, or wait for its timeout to close it (Options → Display; 15 seconds by default). A green checkmark means no exception has been recorded.
        - **No GPS**: no active live GPS source; no current link or fix age can be measured.
        - **GPS replay**: recorded data, with a measured playback cadence; not a live receiver, current satellite health, or current fix age.
        - **Link Ready / transport connected**: connection is open; it does not imply a valid position. **NO FIX**: no accepted fix in this session. **Fresh**: last accepted fix is within its adaptive age limit. **STALE**: the last accepted fix is older than that limit. **Age not reported**: the provider did not supply a trustworthy live arrival time.
        - **Requested/applied Hz**: a receiver setting is confirmed only after readback. **Measured Hz**: actual accepted fixes arriving now. These can differ.
        - **GGA quality 0**: the receiver reports no usable fix. **GSA satellites used**: recent satellite-use evidence; a configured GNSS constellation alone does not prove contribution.
        - **Receiver restore UNVERIFIED**: a temporary receiver change may not have been restored; inspect or reboot the receiver before relying on its configuration.
        - **Gaps / published**: missing subscription sequence entries versus the count of readings published by the sensor service. A gap can indicate a slow consumer, not a GPS fix.
        - **Recording finalizing**: waiting for the file to finish safely. **Recording incomplete**: recording did not finalize cleanly. **Cleanup incomplete**: sensor or receiver cleanup still needs attention.
        - **Replay transport**: play, stop/reset, seek, pause, and timeline controls act on recorded data only; their elapsed times do not describe a live GPS link.

        ## Map status and privacy

        **OSM** means OpenStreetMap. Online tiles require explicit consent and reveal the viewed area to the tile provider. When offline, loading, or unavailable, the route and marker remain visible. This local help document makes no network or location requests.
        """;
}
