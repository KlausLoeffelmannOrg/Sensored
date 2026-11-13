using WarpToolkit.Sensors;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.Drawing.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

/// <summary>
///  Displays every Windows raw sensor stream as an independent page of channel values and gauges.
/// </summary>
internal partial class RawSensorDialog : Form
{
    private readonly ISensorService _sensorService;
    private readonly Func<SensorServiceOptions> _optionsFactory;
    private readonly Func<SensorRecordingRequest> _recordingRequestFactory;
    private readonly ReadingFormats _readingFormats;
    private readonly WindowsRawSensorMonitor _monitor = new();
    private readonly Dictionary<Control, SensorPageState> _sensorPages = [];
    private ISensorServiceSessionLease? _lease;
    private Task? _activationTask;
    private Font? _availabilityFont;
    private Font? _indicatorFont;
    private Font? _toolbarFont;
    private bool _closeStarted;
    private bool _closeCompleted;

    /// <summary>
    ///  Initializes a new instance of the <see cref="RawSensorDialog"/> class.
    /// </summary>
    internal RawSensorDialog(
        ISensorService sensorService,
        Func<SensorServiceOptions> optionsFactory,
        Func<SensorRecordingRequest> recordingRequestFactory,
        ReadingFormats readingFormats,
        DashboardToolbarSize toolbarSize)
    {
        ArgumentNullException.ThrowIfNull(sensorService);
        ArgumentNullException.ThrowIfNull(optionsFactory);
        ArgumentNullException.ThrowIfNull(recordingRequestFactory);

        _sensorService = sensorService;
        _optionsFactory = optionsFactory;
        _recordingRequestFactory = recordingRequestFactory;
        _readingFormats = readingFormats;

        InitializeComponent();
        Disposed += OnRawSensorDialogDisposed;
        ApplyToolbarAppearance(toolbarSize);
        BuildSensorPages();
        BuildRecordingPage();
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Screen screen = Screen.FromControl(Owner ?? this);
        Rectangle workingArea = screen.WorkingArea;
        Size = new Size(
            (int)Math.Round(workingArea.Width * 0.6d),
            (int)Math.Round(workingArea.Height * 0.7d));
        Location = new Point(
            workingArea.Left + ((workingArea.Width - Width) / 2),
            workingArea.Top + ((workingArea.Height - Height) / 2));
    }

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _activationTask = ActivateAsync();
    }

    /// <inheritdoc/>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_closeCompleted)
        {
            e.Cancel = true;

            if (!_closeStarted)
            {
                _closeStarted = true;
                _ = CloseAsync();
            }

            return;
        }

        base.OnFormClosing(e);
    }

    private void ApplyToolbarAppearance(DashboardToolbarSize toolbarSize)
    {
        int symbolSize = (int)toolbarSize;
        SymbolToolStripAppearance.Apply(_toolStrip, new Size(symbolSize, symbolSize));
        _sensorTabs.HeaderSymbolSize = new Size(symbolSize, symbolSize);

        foreach (ToolStripItem item in _toolStrip.Items)
        {
            if (item is not ToolStripSeparator)
            {
                item.DisplayStyle = ToolStripItemDisplayStyle.Image;
            }
        }

        float toolbarFontSize = Math.Max(7f, Font.SizeInPoints * 0.85f);
        _toolbarFont = new Font(
            Font.FontFamily,
            toolbarFontSize,
            Font.Style,
            GraphicsUnit.Point);
        _toolStrip.Font = _toolbarFont;

        _recordingButton.Symbol = SensorDemoSymbols.StartRecording;
        _recordingButton.ToolTipText = "Start a service-owned raw sensor recording";
        _openRecordingButton.Symbol = SensorDemoSymbols.OpenRecordingFolder;
        _openRecordingButton.ToolTipText = "Open the active recording folder";
        _closeButton.Symbol = SensorDemoSymbols.CloseRawSensors;
        _closeButton.ToolTipText = "Close raw sensor diagnostics";
    }

    private async Task ActivateAsync()
    {
        try
        {
            _lease = await _sensorService
                .AcquireSessionAsync(_sensorService.ActiveOptions ?? _optionsFactory())
                .ConfigureAwait(true);

            if (_closeStarted)
            {
                return;
            }

            _refreshTimer.Start();
            SelectCurrentSensor();
            _statusLabel.Text = "Raw readings are shown without calibration, fusion, or axis correction.";
        }
        catch (Exception exception)
        {
            ShowFailure("Could not start raw sensor diagnostics", exception);
        }
    }

    private async Task CloseAsync()
    {
        _refreshTimer.Stop();
        _monitor.Stop();

        try
        {
            await CleanupProgressForm.RunAsync(
                this,
                "Stopping raw sensor monitoring...",
                CompleteCleanupAsync).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowFailure("Could not cleanly close raw sensor diagnostics", exception);
        }
        finally
        {
            _monitor.Dispose();
            _closeCompleted = true;
            Close();
        }
    }

    private async Task ReleaseLeaseAsync()
    {
        if (_lease is not { } lease)
        {
            return;
        }

        _lease = null;
        await lease.DisposeAsync().ConfigureAwait(true);
    }

    private async Task CompleteCleanupAsync()
    {
        if (_activationTask is { } activationTask)
        {
            await activationTask.ConfigureAwait(true);
            _activationTask = null;
        }

        await ReleaseLeaseAsync().ConfigureAwait(true);
    }

    private void BuildSensorPages()
    {
        _availabilityFont = new Font(Font, FontStyle.Bold);
        _indicatorFont = new Font(
            Font.FontFamily,
            Font.SizeInPoints + 2f,
            Font.Style,
            GraphicsUnit.Point);

        foreach (RawSensorDescriptor descriptor in _monitor.Descriptors)
        {
            UserControl page = new()
            {
                AutoScaleMode = AutoScaleMode.Inherit,
                Padding = new Padding(8),
                Tag = descriptor
            };

            TableLayoutPanel layout = new()
            {
                AutoScroll = true,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                RowCount = descriptor.Channels.Count + 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));

            Label availabilityLabel = new()
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = _availabilityFont,
                Text = descriptor.IsAvailable
                    ? $"Available · minimum interval {descriptor.MinimumReportIntervalMilliseconds} ms"
                    : "This sensor is not available on this device."
            };
            layout.Controls.Add(availabilityLabel, 1, 0);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            List<ChannelGaugeState> channels = [];

            for (int index = 0; index < descriptor.Channels.Count; index++)
            {
                RawSensorChannel channel = descriptor.Channels[index];
                AnimatedValueLabel indicator = new()
                {
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Caption = channel.Name,
                    DecimalPlaces = descriptor.Kind is
                        RawSensorKind.AccelerometerStandard
                        or RawSensorKind.AccelerometerLinear
                        or RawSensorKind.AccelerometerGravity
                            ? _readingFormats.AccelerationDigits
                            : _readingFormats.RestDigits,
                    Dimension = channel.Unit,
                    Font = _indicatorFont,
                    Margin = new Padding(3, 12, 3, 4),
                    MaximumValue = channel.Maximum,
                    MinimumValue = channel.Minimum,
                    Padding = new Padding(9, 5, 9, 5),
                    ValueEffectRendering = ValueEffectRendering.Bar
                        | ValueEffectRendering.NewValueInertia
                        | ValueEffectRendering.ResetInertia
                };

                layout.Controls.Add(indicator, 1, index + 1);
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                channels.Add(new ChannelGaugeState(channel, indicator));
            }

            page.Controls.Add(layout);
            _sensorTabs.AddTab(
                descriptor.Caption,
                page,
                GetSensorSymbol(descriptor.Kind));
            _sensorPages.Add(page, new SensorPageState(descriptor, channels));
        }
    }

    private void BuildRecordingPage()
    {
        UserControl page = new()
        {
            AutoScaleMode = AutoScaleMode.Inherit,
            Padding = new Padding(12)
        };
        TableLayoutPanel layout = new()
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        _recordingStateLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 3),
            Text = "No service-owned recording is active."
        };
        _recordingPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        layout.Controls.Add(_recordingStateLabel, 0, 0);
        layout.Controls.Add(_recordingPathTextBox, 0, 1);
        page.Controls.Add(layout);
        _sensorTabs.AddTab("Recording", page, SensorDemoSymbols.RecordingPage);
    }

    private void OnSelectedTabChanged(object? sender, EventArgs e)
        => SelectCurrentSensor();

    private void SelectCurrentSensor()
    {
        if (_sensorTabs.CurrentTab is not { } page
            || !_sensorPages.TryGetValue(page, out SensorPageState? state))
        {
            _monitor.Stop();

            return;
        }

        bool selected = _monitor.Select(state.Descriptor.Kind);
        _statusLabel.Text = selected
            ? $"{state.Descriptor.Caption}: waiting for raw readings..."
            : $"{state.Descriptor.Caption}: unavailable.";
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        UpdateRawPage();
        UpdateRecordingPage();
    }

    private void UpdateRawPage()
    {
        if (_sensorTabs.CurrentTab is not { } page
            || !_sensorPages.TryGetValue(page, out SensorPageState? state)
            || _monitor.LatestReading is not { } reading)
        {
            return;
        }

        int count = Math.Min(state.Channels.Count, reading.Values.Count);

        for (int index = 0; index < count; index++)
        {
            ChannelGaugeState channel = state.Channels[index];
            double value = reading.Values[index];
            channel.Indicator.Value = value;
        }

        _statusLabel.Text =
            $"{state.Descriptor.Caption} · {reading.Timestamp:HH:mm:ss.fff} · "
            + $"{_monitor.MeasuredSampleRateHertz:0.0} Hz";

        if (_monitor.LastException is { } exception)
        {
            _statusLabel.Text += $" · {exception.Message}";
        }
    }

    private void UpdateRecordingPage()
    {
        ISensorRecordingSession? recording = DemoRecordingRegistry.Active;
        _recordingButton.Text = recording is null ? "Start recording" : "Stop recording";
        _recordingButton.Symbol = recording is null
            ? SensorDemoSymbols.StartRecording
            : SensorDemoSymbols.StopRecording;
        _recordingButton.ToolTipText = recording is null
            ? "Start a service-owned raw sensor recording"
            : "Stop the active raw sensor recording";
        _openRecordingButton.Enabled = recording is not null;

        if (recording is null)
        {
            _recordingStateLabel.Text = "No service-owned recording is active.";
            _recordingPathTextBox.Clear();

            return;
        }

        SensorRecordingStatus status = recording.Status;
        _recordingStateLabel.Text =
            $"Rows {status.RowsWritten:N0} · dropped {status.RowsDropped:N0} · "
            + $"part {status.ActivePartIndex:N0}";
        _recordingPathTextBox.Text = status.FolderPath;
    }

    private async void OnRecordingClick(object? sender, EventArgs e)
    {
        _recordingButton.Enabled = false;

        try
        {
            if (DemoRecordingRegistry.Active is { } active)
            {
                DemoRecordingRegistry.Set(null);
                await active.StopAsync().ConfigureAwait(true);
            }
            else
            {
                ISensorRecordingSession recording = await _sensorService
                    .StartRecordingAsync(_recordingRequestFactory())
                    .ConfigureAwait(true);
                DemoRecordingRegistry.Set(recording);
            }
        }
        catch (Exception exception)
        {
            ShowFailure("Recording operation failed", exception);
        }
        finally
        {
            _recordingButton.Enabled = true;
            UpdateRecordingPage();
        }
    }

    private void OnOpenRecordingClick(object? sender, EventArgs e)
    {
        if (DemoRecordingRegistry.Active?.FolderPath is not { } folderPath)
        {
            return;
        }

        try
        {
            using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(folderPath)
                {
                    UseShellExecute = true
                });
        }
        catch (Exception exception)
        {
            ShowFailure("Could not open the recording folder", exception);
        }
    }

    private void OnCloseClick(object? sender, EventArgs e)
        => Close();

    private void ShowFailure(string operation, Exception exception)
    {
        _statusLabel.Text = $"{operation}: {exception.Message}";
        MessageBox.Show(
            this,
            exception.Message,
            operation,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static SvgSymbol GetSensorSymbol(RawSensorKind kind)
        => kind switch
        {
            RawSensorKind.AccelerometerStandard => SensorDemoSymbols.Accelerometer,
            RawSensorKind.AccelerometerLinear => SensorDemoSymbols.LinearAcceleration,
            RawSensorKind.AccelerometerGravity => SensorDemoSymbols.Gravity,
            RawSensorKind.Gyrometer => SensorDemoSymbols.Gyrometer,
            RawSensorKind.Inclinometer => SensorDemoSymbols.Inclinometer,
            RawSensorKind.OrientationAbsolute => SensorDemoSymbols.OrientationAbsolute,
            RawSensorKind.OrientationRelative => SensorDemoSymbols.OrientationRelative,
            RawSensorKind.Compass => SensorDemoSymbols.Compass,
            RawSensorKind.LightSensor => SensorDemoSymbols.LightSensor,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unsupported raw sensor kind.")
        };

    private void OnRawSensorDialogDisposed(object? sender, EventArgs e)
    {
        Disposed -= OnRawSensorDialogDisposed;
        _availabilityFont?.Dispose();
        _availabilityFont = null;
        _indicatorFont?.Dispose();
        _indicatorFont = null;
        _toolbarFont?.Dispose();
        _toolbarFont = null;
    }

    /// <summary>
    ///  Holds the controls associated with one raw sensor page.
    /// </summary>
    private sealed record SensorPageState(
        RawSensorDescriptor Descriptor,
        IReadOnlyList<ChannelGaugeState> Channels);

    /// <summary>
    ///  Holds one raw channel descriptor and its numeric gauge controls.
    /// </summary>
    private sealed record ChannelGaugeState(
        RawSensorChannel Descriptor,
        AnimatedValueLabel Indicator);
}
