using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <content>Live shutdown log and Mercedes diagnostic presentation.</content>
public partial class MainForm
{
    private RichTextBox? _shutdownLog;
    private Font? _shutdownFont;
    private ILogger<MainForm>? _shutdownLogger;
    private static readonly PropertyInfo? VehicleTelemetryProperty = typeof(SensorHub).GetProperty(
        "LatestVehicleTelemetry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? ObdField = typeof(SensorHub).GetField(
        "_obd", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>Shows the selectable shutdown log without disabling the shell.</summary>
    internal void ShowShutdownTerminal()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_shutdownLog is not null)
        {
            _shutdownLog.Visible = true;
            _shutdownLog.BringToFront();

            return;
        }

        _shutdownFont = CreateTerminalFont();
        _shutdownLog = new RichTextBox
        {
            Name = "_shutdownLog",
            Dock = DockStyle.Fill,
            Font = _shutdownFont,
            ReadOnly = true,
            HideSelection = false,
            DetectUrls = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(16, 18, 22),
            ForeColor = Color.FromArgb(220, 224, 230),
            TabStop = true
        };
        Controls.Add(_shutdownLog);
        _shutdownLog.BringToFront();
    }

    /// <summary>Appends one shutdown stage and writes it to the hosted application logger.</summary>
    /// <param name="message">The stage text already produced by the acquisition owner.</param>
    internal void AppendShutdown(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        bool error = message.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        bool warning = !error && message.Contains("WARNING", StringComparison.OrdinalIgnoreCase);
        if (error || warning)
        {
            _shutdownHadIssue = true;
        }

        LogShutdown(message, error, warning);
        if (_shutdownLog is null || _shutdownLog.IsDisposed)
        {
            return;
        }

        _shutdownLog.SelectionStart = _shutdownLog.TextLength;
        _shutdownLog.SelectionLength = 0;
        _shutdownLog.SelectionColor = error
            ? Color.FromArgb(255, 128, 128)
            : warning
                ? Color.FromArgb(255, 196, 96)
                : _shutdownLog.ForeColor;
        _shutdownLog.AppendText(message + Environment.NewLine);
        _shutdownLog.SelectionStart = _shutdownLog.TextLength;
        _shutdownLog.ScrollToCaret();
    }

    private void OnShutdownProgress(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                Invoke(() => AppendShutdown(message));
            }
            catch (ObjectDisposedException)
            {
            }

            return;
        }

        AppendShutdown(message);
    }

    private void OnCanDiagnostic(ObdDiagnosticEntry entry)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                Invoke(() => _instruments.AppendCanDiagnostic(entry));
            }
            catch (ObjectDisposedException)
            {
            }

            return;
        }

        _instruments.AppendCanDiagnostic(entry);
    }

    private VehicleTelemetryPayload? ReadVehicleTelemetry()
    {
        if (VehicleTelemetryProperty?.GetValue(_hub) is VehicleTelemetryPayload published)
        {
            return published;
        }

        object? source = ObdField?.GetValue(_hub);

        return source?.GetType().GetProperty(
            "LatestTelemetry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(source) as VehicleTelemetryPayload;
    }

    private void LogShutdown(string message, bool error, bool warning)
    {
        _shutdownLogger ??= _serviceProvider?.GetService<ILogger<MainForm>>();
        if (_shutdownLogger is null)
        {
            return;
        }

        if (error)
        {
            _shutdownLogger.LogError("Shutdown: {Message}", message);
        }
        else if (warning)
        {
            _shutdownLogger.LogWarning("Shutdown: {Message}", message);
        }
        else
        {
            _shutdownLogger.LogInformation("Shutdown: {Message}", message);
        }
    }

    private static Font CreateTerminalFont()
    {
        foreach (string family in new[] { "Cascadia Mono", "Cascadia Code", "Consolas" })
        {
            Font candidate = new(family, 12f, FontStyle.Regular, GraphicsUnit.Point);
            if (candidate.FontFamily.Name.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            candidate.Dispose();
        }

        return new Font(FontFamily.GenericMonospace, 12f, FontStyle.Regular, GraphicsUnit.Point);
    }
}
