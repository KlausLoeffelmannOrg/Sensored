namespace Sensored;

/// <summary>Describes an optional ordinary-driving opportunity, not a sensor-verified maneuver.</summary>
internal sealed record DrivingCaptureTask(
    string Id, string Title, string Opportunity, string Collection, string Purpose)
{
    /// <summary>Gets the opt-in task catalog, starting with unguided recording.</summary>
    internal static IReadOnlyList<DrivingCaptureTask> All { get; } = Array.AsReadOnly<DrivingCaptureTask>(
    [
        new("off", "Off - unguided recording",
            "No driving task is selected.",
            "Choose one upcoming road condition you already expect on your route. "
                + "Read the guidance and add any known conditions or uncertainties before selecting Use task. "
                + "Then use the normal Start and Record commands; selecting a task never starts recording.",
            "Optional task labels make useful examples easier to find later without changing acquisition or fusion."),
        new("parked-baseline", "Parked baseline",
            "A safe, legal parking place before or after your drive.",
            "If convenient, record about 30 seconds with the tablet secured and untouched. "
                + "Note whether the vehicle is level or on a slope, and whether that is measured or only estimated.",
            "Compare settled gravity, gyro behavior and reported zero speed. A quiet tablet alone does not prove a stop or level road."),
        new("steady-straight", "Steady straight road",
            "An ordinary straight stretch, approximately level if known.",
            "Record roughly 30-60 seconds of normal steady travel only if traffic naturally allows it. "
                + "Keep a normal legal speed; do not watch the gauges. Note slope uncertainty and obvious vibration.",
            "Compare GPS and OBD speed, source timing and steady attitude without much turn or acceleration evidence."),
        new("normal-speed-change", "Normal speed changes",
            "A straight section where ordinary traffic already calls for changing speed.",
            "Capture normal acceleration, coasting or braking as required by the route. "
                + "Do not create braking events, accelerate hard, or change following distance for the recording.",
            "Distinguish longitudinal acceleration and source latency from false hill or pitch indications."),
        new("gentle-bends", "Gentle bends",
            "Naturally occurring left or right bends on your normal route.",
            "Drive normally at an appropriate safe speed. Note turn direction and whether the road seems level, "
                + "sloped or banked; unknown is useful too. A few ordinary bends are enough for a first example.",
            "Investigate turns being displayed as pitch or roll and course wrap behavior. Road bank is not independently measured."),
        new("straight-hill", "Straight hill",
            "A straight uphill or downhill section already on your route.",
            "Record an ordinary ascent or descent. Note up/down, speed changes, and whether any grade reference "
                + "is independent or merely estimated. An opposite-direction passage is optional only if already convenient.",
            "Separate conditional body pitch from acceleration; an estimated road slope is not angular ground truth."),
        new("hill-bends", "Hill with bends",
            "A normal uphill or downhill road that also curves.",
            "Follow the route normally, with safe braking before bends when needed. "
                + "Note up/down and left/right when known. Skip the task if visibility, traffic or weather is unsuitable.",
            "Collect compound turn-and-climb examples where lateral acceleration can leak into pitch or roll."),
        new("uneven-surface", "Ordinary uneven surface",
            "An uneven surface you would normally traverse.",
            "Make one routine passage at an appropriate safe speed. Do not aim at potholes, bumps or obstacles. "
                + "Note the surface and whether the tablet or mount visibly moved, after parking.",
            "Distinguish ordinary road vibration from faulty spikes and mounting movement."),
        new("gps-cover", "Natural GPS obstruction",
            "Existing tree cover, buildings or a tunnel on your normal route.",
            "Drive through normally. Do not unplug, shield or reconfigure devices while driving. "
                + "If convenient, retain some open-sky data before and after the covered section.",
            "Inspect missing or stale GPS, recovery timing and OBD continuity without inventing replacement measurements.")
    ]);

    /// <summary>Gets the collection instructions and shared preparation/handoff reminders.</summary>
    internal string Guidance
        => $"OPPORTUNITY\r\n{Opportunity}\r\n\r\nWHAT TO COLLECT\r\n{Collection}\r\n\r\nWHY IT HELPS\r\n{Purpose}"
            + "\r\n\r\nBEFORE / AFTER\r\nSecure the tablet. Check GPS fix age and measured rate; note any missing sources. "
            + "10 Hz RawV2 is a useful starting setting, not a promise of GPS rate. "
            + "Keep a short parked settling interval. After parking, stop recording, wait for finalization and retain the whole recording folder.";
}

/// <summary>Holds user-declared recording intent; it is not evidence that the conditions occurred.</summary>
internal sealed record DrivingTaskSelection(DrivingCaptureTask Task, string Notes)
{
    /// <summary>Formats a user-declared intent boundary, never a ground-truth observation.</summary>
    internal string MarkerLabel(string boundary)
        => $"driving-task-v1 | {boundary} | {Task.Id} | {Task.Title} | "
            + $"Expected conditions only; not sensor-verified. Notes: {Notes}";
}
