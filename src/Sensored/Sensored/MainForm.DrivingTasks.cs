using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

/// <summary>Attaches optional user-declared task intervals to the existing recording markers.</summary>
public partial class MainForm
{
    private readonly SymbolToolStripButton _drivingTasksButton = new()
    {
        Name = "_drivingTasksButton",
        Text = "Driving tasks"
    };

    private DrivingTaskSelection? _selectedDrivingTask;
    private DrivingTaskSelection? _recordedDrivingTask;

    private void ConfigureDrivingTasksCommand(int symbolSize)
    {
        _drivingTasksButton.ConfigureItem(_toolStrip, FluentSymbols.AllSymbols.OpenFile,
            (OnDrivingTasksCommand, true), "Choose an optional driving-data task", symbolSize);
        UpdateDrivingTasksCommand();
    }

    private void UpdateDrivingTasksCommand()
    {
        _drivingTasksButton.Enabled = !_transitioning && !_shutdownStarted;
        _drivingTasksButton.Checked = _selectedDrivingTask is not null;
        _drivingTasksButton.Text = _selectedDrivingTask is null ? "Driving tasks" : "Driving task ON";
        _drivingTasksButton.ToolTipText = _selectedDrivingTask is not { } selection
            ? "Optional guide: choose an upcoming road condition while parked. Off by default."
            : $"{selection.Task.Title}: "
                + (_v2Recording?.Fault is { } fault
                    ? $"task annotation unavailable: {fault.Message}."
                    : _recordedDrivingTask is not null
                    ? "intent label attached to current recording."
                    : "armed for the next live recording; not a verified observation.")
                + $"\r\n{selection.Task.Guidance}\r\nNotes: {selection.Notes}";
    }

    private void OnDrivingTasksCommand(object? sender, EventArgs e)
    {
        if (_transitioning || _shutdownStarted)
        {
            return;
        }

        try
        {
            using DrivingTaskDialog dialog = new(_selectedDrivingTask,
                _recording is { Status.IsActive: true } && _replayPlayer is null, Font);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ApplyDrivingTaskSelection(dialog.Selection);
            }
        }
        catch (Exception exception)
        {
            ShowError("Could not update the driving task", exception);
        }
    }

    private void ApplyDrivingTaskSelection(DrivingTaskSelection? selection)
    {
        if (selection == _selectedDrivingTask)
        {
            return;
        }

        lock (_obdRecordingGate)
        {
            if (_v2Recording is { } pair && _recording is { Status.IsActive: true }
                && _recordingStopTask is null && _replayPlayer is null)
            {
                if (_recordedDrivingTask is { } previous)
                {
                    AddDrivingTaskMarker(pair, previous, "intent-end (selection changed)");
                }

                _recordedDrivingTask = null;
                if (selection is not null)
                {
                    AddDrivingTaskMarker(pair, selection, "intent-start (selection changed)");
                    _recordedDrivingTask = selection;
                }
            }

            _selectedDrivingTask = selection;
        }

        UpdateDrivingTasksCommand();
        SetStatusAndLog(selection is null ? "Driving task off; recording is unchanged."
            : $"Driving task selected: {selection.Task.Title}. Expected conditions only; recording is unchanged.");
    }

    private void BeginDrivingTaskRecording()
    {
        lock (_obdRecordingGate)
        {
            _recordedDrivingTask = null;
            if (_selectedDrivingTask is { } selection && _v2Recording is { } pair)
            {
                AddDrivingTaskMarker(pair, selection, "intent-start (recording started)");
                _recordedDrivingTask = selection;
            }
        }
    }

    private void EndDrivingTaskRecording()
    {
        lock (_obdRecordingGate)
        {
            if (_recordedDrivingTask is { } selection && _v2Recording is { } pair)
            {
                AddDrivingTaskMarker(pair, selection, "intent-end (recording stopping; not task completion)");
            }

            _recordedDrivingTask = null;
        }
    }

    private void AddDrivingTaskMarker(SensorV2Recording pair, DrivingTaskSelection selection, string boundary)
        => pair.AddMarker(new(selection.MarkerLabel(boundary),
            DateTimeOffset.UtcNow, pair.LastInputSequence, _fusionSession?.Epoch ?? 0));
}
