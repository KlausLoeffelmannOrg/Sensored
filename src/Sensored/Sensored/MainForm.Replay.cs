using System.Diagnostics;
using WarpToolkit.WinForms.Drawing.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

/// <summary>
///  Hosts replay sequencing and status-strip transport behavior.
/// </summary>
public partial class MainForm
{
    private static readonly TimeSpan SlowSeekInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FastSeekInterval = TimeSpan.FromSeconds(30);

    private SensorReplaySequence? _replaySequence;
    private int _replayPartIndex;
    private int _replayInitialPartIndex;
    private int _replayStopStage;
    private TimeSpan _replayStoppedPosition;
    private bool _replayStatusHovered;
    private bool _replayFlashPhase;

    private SensorReplayPartInfo? CurrentReplayPart
        => _replaySequence is { } sequence
            && _replayPartIndex >= 0
            && _replayPartIndex < sequence.Parts.Count
                ? sequence.Parts[_replayPartIndex]
                : null;

    private void SetupReplayStatus()
    {
        _statusStrip.SymbolScalingMode = ToolStripSymbolScalingMode.FontSizeSquare;
        ConfigureReplayStatusSymbol(_replayPlayStatusButton, SensorDemoSymbols.ReplayPlay, "Play recording");
        ConfigureReplayStatusSymbol(_replayStopStatusButton, SensorDemoSymbols.ReplayStop, "Stop replay; press again to reset");
        ConfigureReplayStatusSymbol(_replayFastBackwardButton, SensorDemoSymbols.ReplayBackFast, "Back thirty seconds");
        ConfigureReplayStatusSymbol(_replaySlowBackwardButton, SensorDemoSymbols.ReplayBack, "Back five seconds");
        ConfigureReplayStatusSymbol(_replayPauseStatusButton, SensorDemoSymbols.ReplayPause, "Pause replay");
        ConfigureReplayStatusSymbol(_replaySlowForwardButton, SensorDemoSymbols.ReplayForward, "Forward five seconds");
        ConfigureReplayStatusSymbol(_replayFastForwardButton, SensorDemoSymbols.ReplayForwardFast, "Forward thirty seconds");
        _statusTimer.Interval = 500;
        _statusStrip.MouseEnter += OnReplayStatusMouseEnter;
        _statusStrip.MouseLeave += OnReplayStatusMouseLeave;
        UpdateReplayStatus();
    }

    private static void ConfigureReplayStatusSymbol(
        SymbolStatusStripCommand item,
        SvgSymbol symbol,
        string caption)
    {
        item.Symbol = symbol;
        item.DisplayStyle = ToolStripItemDisplayStyle.Image;
        item.Text = caption;
        item.AccessibleName = caption;
        item.ToolTipText = caption;
    }

    private async Task StartReplayPartAsync(
        int partIndex,
        TimeSpan position,
        bool pauseAfterStart,
        bool preservePresentation,
        FusionContinuity fusionContinuity = FusionContinuity.Reset)
    {
        if (_sensorSourceFactory is null || _replaySequence is not { } sequence)
        {
            throw new InvalidOperationException("Replay has not been configured.");
        }

        if (_lease is not null)
        {
            _transitioning = true;
            UpdateCommandStates();

            try
            {
                await StopSensorSessionCoreAsync(!preservePresentation).ConfigureAwait(true);
            }
            finally
            {
                _transitioning = false;
            }
        }
        else if (!preservePresentation)
        {
            ResetSessionPresentation();
        }

        SensorReplayPartInfo part = sequence.Parts[partIndex];
        TimeSpan normalizedPosition = position < TimeSpan.Zero
            ? TimeSpan.Zero
            : position > part.Duration
                ? part.Duration
                : position;
        _replayPartIndex = partIndex;
        _replayStoppedPosition = normalizedPosition;
        _replayStopStage = 0;
        _hasCompletedReplayPresentation = false;
        await _sensorSourceFactory.ConfigureReplayAsync(
            part.Path, normalizedPosition, _options.ReplaySpeedPercent / 100d).ConfigureAwait(true);

        try
        {
            await StartSensorSessionAsync(part.Path, fusionContinuity).ConfigureAwait(true);

            if (pauseAfterStart)
            {
                _replayPlayer?.Pause();
            }
        }
        catch
        {
            await _sensorSourceFactory.ConfigureAsync(_options.CreateWindowsSourceOptions()).ConfigureAwait(true);
            throw;
        }

        UpdateReplayStatus();
    }

    private async Task SeekReplayAsync(TimeSpan position)
    {
        SensorReplayPartInfo? part = CurrentReplayPart;

        if (part is null)
        {
            return;
        }

        bool pauseAfterStart = _replayPlayer?.IsPaused == true;
        TimeSpan normalizedPosition = position < TimeSpan.Zero
            ? TimeSpan.Zero
            : position > part.Duration
                ? part.Duration
                : position;

        await StartReplayPartAsync(
            _replayPartIndex,
            normalizedPosition,
            pauseAfterStart,
            preservePresentation: false,
            fusionContinuity: FusionContinuity.Reseek).ConfigureAwait(true);
    }

    private async void OnReplayPlayStatusClick(object? sender, EventArgs e)
    {
        try
        {
            if (_replayPlayer is { IsPaused: true } pausedPlayer)
            {
                pausedPlayer.Resume();
                _replayStopStage = 0;
                UpdateReplayStatus();

                return;
            }

            TimeSpan position = _replayPlayer is null
                ? _replayStoppedPosition
                : TimeSpan.Zero;
            await StartReplayPartAsync(
                _replayPartIndex,
                position,
                pauseAfterStart: false,
                preservePresentation: false,
                fusionContinuity: FusionContinuity.Reseek).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not play the recording", exception);
        }
    }

    private async void OnReplayStopStatusClick(object? sender, EventArgs e)
    {
        try
        {
            if (_replayPlayer is { } player)
            {
                _replayStoppedPosition = player.Position;
                _transitioning = true;
                UpdateCommandStates();

                try
                {
                    await StopSensorSessionCoreAsync(resetPresentation: false).ConfigureAwait(true);
                }
                finally
                {
                    _transitioning = false;
                }

                _replayStopStage = 1;
                SetStatus("Replay stopped. Stop again to reset this part.");
            }
            else if (_replayStopStage <= 1)
            {
                _replayStoppedPosition = TimeSpan.Zero;
                _replayStopStage = 2;
                ResetSessionPresentation();
                if (_sensorSourceFactory is { } factory)
                {
                    await factory.ConfigureAsync(_options.CreateWindowsSourceOptions()).ConfigureAwait(true);
                }
                SetStatus("Replay reset to the start of this part.");
            }
            else
            {
                _replayPartIndex = _replayInitialPartIndex;
                _replayStoppedPosition = TimeSpan.Zero;
                _replayStopStage = 3;
                ResetSessionPresentation();
                if (_sensorSourceFactory is { } factory)
                {
                    await factory.ConfigureAsync(_options.CreateWindowsSourceOptions()).ConfigureAwait(true);
                }
                SetStatus("Replay reset to the originally selected part and time.");
            }

            UpdateCommandStates();
            UpdateReplayStatus();
        }
        catch (Exception exception)
        {
            ShowError("Could not stop the recording replay", exception);
        }
    }

    private void OnReplayPauseStatusClick(object? sender, EventArgs e)
    {
        if (_replayPlayer is not { } player)
        {
            return;
        }

        if (player.IsPaused)
        {
            player.Resume();
            SetStatus("Replay resumed.");
        }
        else
        {
            player.Pause();
            SetStatus("Replay paused.");
        }

        UpdateReplayStatus();
    }

    private async void OnReplayFastBackwardClick(object? sender, EventArgs e)
        => await SeekFromCommandAsync(-FastSeekInterval).ConfigureAwait(true);

    private async void OnReplaySlowBackwardClick(object? sender, EventArgs e)
        => await SeekFromCommandAsync(-SlowSeekInterval).ConfigureAwait(true);

    private async void OnReplaySlowForwardClick(object? sender, EventArgs e)
        => await SeekFromCommandAsync(SlowSeekInterval).ConfigureAwait(true);

    private async void OnReplayFastForwardClick(object? sender, EventArgs e)
        => await SeekFromCommandAsync(FastSeekInterval).ConfigureAwait(true);

    private async Task SeekFromCommandAsync(TimeSpan delta)
    {
        try
        {
            TimeSpan position = _replayPlayer?.Position ?? _replayStoppedPosition;
            await SeekReplayAsync(position + delta).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not move within the recording", exception);
        }
    }

    private async void OnReplayTimeMenuClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripItem { Tag: TimeSpan position })
        {
            return;
        }

        try
        {
            await SeekReplayAsync(position).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not seek within the recording", exception);
        }
    }

    private void OnReplayFileDoubleClick(object? sender, EventArgs e)
    {
        if (CurrentReplayPart is not { } part)
        {
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new("explorer.exe")
            {
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add($"/select,{part.Path}");
            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            ShowError("Could not open the replay folder", exception);
        }
    }

    private void OnReplayStatusMouseEnter(object? sender, EventArgs e)
    {
        _replayStatusHovered = true;
        UpdateReplayStatus();
    }

    private void OnReplayStatusMouseLeave(object? sender, EventArgs e)
    {
        _replayStatusHovered = false;
        UpdateReplayStatus();
    }

    private void UpdateReplayStatus()
    {
        SensorReplayPartInfo? part = CurrentReplayPart;
        bool hasReplay = part is not null;
        bool compactVisible = hasReplay
            && (!_options.HideReplayControls || _replayStatusHovered || _replayTimeButton.DropDown.Visible);
        bool fullVisible = compactVisible && !_options.HideReplayControls;

        _statusLabel.Visible = !hasReplay;
        _obdStatusLabel.Visible = !hasReplay;
        _rawStatusLabel.Visible = !hasReplay;
        _statusSpring.Visible = !hasReplay;
        _replayPlayStatusButton.Visible = compactVisible;
        _replayStopStatusButton.Visible = compactVisible;
        _replayPauseStatusButton.Visible = compactVisible;
        _replayTimeButton.Visible = compactVisible;
        _replayBackwardSeparator.Visible = fullVisible;
        _replayFastBackwardButton.Visible = fullVisible;
        _replaySlowBackwardButton.Visible = fullVisible;
        _replayPauseSeparator.Visible = fullVisible;
        _replayForwardSeparator.Visible = fullVisible;
        _replaySlowForwardButton.Visible = fullVisible;
        _replayFastForwardButton.Visible = fullVisible;
        _replayTimeSeparator.Visible = fullVisible;
        _replayFileLabel.Visible = hasReplay;

        if (part is null || _replaySequence is null)
        {
            return;
        }

        TimeSpan position = _replayPlayer?.Position ?? _replayStoppedPosition;
        _replayTimeButton.Text = FormatReplayTime(position);
        _replayFileLabel.Text =
            $"{Path.GetFileName(part.Path)} - part {_replayPartIndex + 1} "
            + $"of {_replaySequence.Parts.Count}";
        _replayFileLabel.ToolTipText = part.Path;
        bool commandsAvailable = !_transitioning && !_shutdownStarted;
        _replayPauseStatusButton.Enabled = commandsAvailable && _replayPlayer is not null;
        bool paused = _replayPlayer?.IsPaused == true;
        ConfigureReplayStatusSymbol(
            _replayPauseStatusButton,
            paused ? SensorDemoSymbols.ReplayResume : SensorDemoSymbols.ReplayPause,
            paused ? "Resume replay" : "Pause replay");
        _replayPlayStatusButton.Enabled = commandsAvailable;
        _replayStopStatusButton.Enabled = commandsAvailable;
        _replayFastBackwardButton.Enabled = commandsAvailable;
        _replaySlowBackwardButton.Enabled = commandsAvailable;
        _replaySlowForwardButton.Enabled = commandsAvailable;
        _replayFastForwardButton.Enabled = commandsAvailable;
        _replayTimeButton.Enabled = commandsAvailable;

        SetReplayTimeMenu(_replay20MenuItem, part.Duration * 0.2d, "20%");
        SetReplayTimeMenu(_replay40MenuItem, part.Duration * 0.4d, "40%");
        SetReplayTimeMenu(_replay60MenuItem, part.Duration * 0.6d, "60%");
        SetReplayTimeMenu(_replay80MenuItem, part.Duration * 0.8d, "80%");
        SetReplayTimeMenu(_replayFirstGpsMenuItem, part.FirstGpsChange, "First GPS change");
        SetReplayTimeMenu(_replayLastGpsMenuItem, part.LastGpsChange, "Last GPS change");
        SetReplayTimeMenu(_replayResetMenuItem, TimeSpan.Zero, "Reset");

        bool activelyPlaying = _replayPlayer is { IsPaused: false };
        _replayFlashPhase = activelyPlaying && !_replayFlashPhase;
        _replayPlayStatusButton.BackColor = activelyPlaying && _replayFlashPhase
            ? SystemColors.GradientActiveCaption
            : SystemColors.Control;
    }

    private static void SetReplayTimeMenu(
        ToolStripMenuItem item,
        TimeSpan? position,
        string caption)
    {
        item.Enabled = position is not null;
        item.Tag = position;
        item.Text = $"{FormatReplayTime(position ?? TimeSpan.Zero)} - {caption}";
    }

    private static string FormatReplayTime(TimeSpan position)
    {
        int totalMinutes = Math.Max(0, (int)position.TotalMinutes);
        int frames = Math.Clamp(position.Milliseconds / 10, 0, 99);

        return $"{totalMinutes:000}:{position.Seconds:00}:{frames:00}";
    }
}
