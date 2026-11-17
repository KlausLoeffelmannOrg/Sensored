namespace Sensored;

/// <summary>Collects large-button retrospective labels while parked, never unsolicited while driving.</summary>
internal sealed partial class FusionFeedbackDialog : Form
{
    private readonly Func<bool> _canReview;

    /// <summary>Creates a parked-only editor for retained episodes from any prior session.</summary>
    internal FusionFeedbackDialog(FusionLearningEpisode[] episodes, string progress, Func<bool> canReview, Font font)
    {
        _canReview = canReview;
        InitializeComponent();
        Font = font;
        _progress.Text = progress;
        _episodes.DisplayMember = nameof(FusionLearningEpisode.End);
        _episodes.Format += OnFormatEpisode;
        _episodes.DataSource = episodes;
        OnEpisodeChanged(this, EventArgs.Empty);
        OnSafetyTick(this, EventArgs.Empty);
        _safetyTimer.Start();
    }

    /// <summary>Gets the historical episode selected for annotation.</summary>
    internal FusionLearningEpisode? SelectedEpisode
        => _episodes.SelectedItem as FusionLearningEpisode;

    /// <summary>Gets independent category choices, preserving unknown rather than preselecting road truth.</summary>
    internal FusionRoadFeedback CreateFeedback()
        => new(_accelNone.Checked ? "none" : _accelSlight.Checked ? "slight"
                : _accelMedium.Checked ? "medium" : _accelFull.Checked ? "full-throttle" : "unknown",
            _inclineNone.Checked ? "none" : _inclineLight.Checked ? "light"
                : _inclineMedium.Checked ? "medium" : _inclineSteep.Checked ? "steep" : "unknown",
            _hardTurn.Checked, _braking.Checked, _notes.Text.Trim());

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Size area = Screen.FromControl(this).WorkingArea.Size;
        MinimumSize = new(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
        Size = new(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
    }

    private void OnFormatEpisode(object? sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is FusionLearningEpisode episode)
        {
            e.Value = $"{episode.End.ToLocalTime():g} - {episode.Reason}";
        }
    }

    private void OnEpisodeChanged(object? sender, EventArgs e)
    {
        FusionLearningEpisode? episode = SelectedEpisode;
        _question.Text = episode is null
            ? "No unanswered episodes. Start a live session; support losses and matched ambiguities are queued automatically."
            : $"{episode.Start.ToLocalTime():G} to {episode.End.ToLocalTime():G}\r\n"
                + $"Observed gate: {episode.Reason}\r\n"
                + $"OBD acceleration: {episode.OdometerAcceleration:0.00}; fusion: {episode.FusionAcceleration:0.00} m/s2 "
                + "(blank means unavailable).\r\n"
                + "What happened in THIS interval? An incline, propulsion, braking, a turn or bias can look similar. "
                + "Choose across categories, or leave Unknown if you cannot remember.\r\n"
                + $"Session: {episode.SessionId}; epoch {episode.Epoch}.\r\n"
                + (episode.HasCanonicalSequenceRange
                    ? $"IMU input {episode.StartSequence}..{episode.EndSequence}.\r\n"
                    : $"Legacy endpoints {episode.StartSequence}, {episode.EndSequence}: different streams, not an ordered range.\r\n")
                + $"Original profile: {episode.ProfileId}; IMU: {episode.SourceId ?? "unknown"}, generation {episode.SourceGeneration}.\r\n"
                + $"Recording: {episode.RecordingPath ?? "not recording; no raw companion"}";
        _accelUnknown.Checked = true;
        _inclineUnknown.Checked = true;
        _hardTurn.Checked = false;
        _braking.Checked = false;
        _notes.Clear();
        _parked.Checked = false;
        OnSafetyTick(this, EventArgs.Empty);
    }

    private void OnSafetyTick(object? sender, EventArgs e)
    {
        bool safe = _canReview();
        _safety.Text = safe
            ? "Review only while parked. These are your observations, NOT measured road truth. Answers are saved across restarts; they do not yet train calibration."
            : "Review locked: motion, stale input or a session transition detected. Stop safely before using this screen.";
        _choices.Enabled = safe && SelectedEpisode is not null;
        _episodes.Enabled = safe;
        _save.Enabled = safe && _parked.Checked && SelectedEpisode is not null;
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        OnSafetyTick(this, EventArgs.Empty);
        if (_save.Enabled)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
