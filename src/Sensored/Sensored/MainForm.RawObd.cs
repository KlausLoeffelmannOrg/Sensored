namespace Sensored;

/// <summary>Connects background raw OBD capture to the lazily refreshed fourth tab.</summary>
public partial class MainForm
{
    private ObdRawTrafficCollector? _rawObd;
    private Exception? _observedRawObdFault;
    private bool _rawObdCommandPending;

    private ObdRawTrafficCollector RawObd
        => _rawObd ??= new ObdRawTrafficCollector();

    private void PresentRawObd()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (_rawObd?.Fault is { } fault && !ReferenceEquals(fault, _observedRawObdFault))
        {
            _observedRawObdFault = fault;
            ShowError("Raw OBD capture failed; any retained file may be incomplete", fault);
        }

        if (!ReferenceEquals(_bottomTabControl.CurrentTab, _rawObdTabHost))
        {
            return;
        }

        _rawObdTabHost.Present(_rawObd?.ReadSnapshot() ?? new(0, [], false, null),
            !_shutdownStarted && !_obdReplayActive && _obdSource?.Latest.Connected == true
                && _rawObd?.Fault is null && !_rawObdCommandPending, _rawObd?.Fault?.Message);
        _rawObdTabHost.SetPending(_rawObdCommandPending);
    }

    private async void OnRawObdRecordRequested(object? sender, EventArgs e)
    {
        if (_rawObdCommandPending || _shutdownStarted || _obdReplayActive
            || _obdSource?.Latest.Connected != true)
        {
            return;
        }

        _rawObdCommandPending = true;
        PresentRawObd();
        try
        {
            string path = Path.Combine(_options.RecordingRootFolder, "RawObd",
                $"obd-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.jsonl");
            await RawObd.StartRecordingAsync(path).ConfigureAwait(true);
            _log.Information($"Raw OBD recording started: {path}; automatic stop after one minute.");
        }
        catch (Exception exception)
        {
            ShowError("Cannot start raw OBD recording", exception);
        }
        finally
        {
            _rawObdCommandPending = false;
            PresentRawObd();
        }
    }

    private async void OnRawObdStopRequested(object? sender, EventArgs e)
    {
        if (_rawObdCommandPending || _rawObd is null)
        {
            return;
        }

        _rawObdCommandPending = true;
        PresentRawObd();
        try
        {
            await _rawObd.StopRecordingAsync().ConfigureAwait(true);
            _log.Information("Raw OBD recording stopped and flushed.");
        }
        catch (Exception exception)
        {
            ShowError("Cannot finalize raw OBD recording", exception);
        }
        finally
        {
            _rawObdCommandPending = false;
            PresentRawObd();
        }
    }

    private async Task StopRawObdAsync()
    {
        if (_rawObd is { } collector)
        {
            await collector.DisposeAsync().ConfigureAwait(true);
            _rawObd = null;
        }
    }
}
