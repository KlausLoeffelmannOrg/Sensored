using System.Globalization;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored.TabViews;

/// <summary>Compares GPS motion and displays independent raw device sensor readings.</summary>
public partial class GpsMotionTabView : UserControl
{
    private ReadingFormats _formats = ReadingFormats.Default;
    private readonly bool _initialized;

    /// <summary>Initializes the raw data and GPS motion diagnostic view.</summary>
    public GpsMotionTabView()
    {
        InitializeComponent();
        _motionGrid.Rows.Add("Fusion");
        _motionGrid.Rows.Add("GPS receiver");
        _motionGrid.Rows.Add("2 positions");
        _motionGrid.Rows.Add("4 positions");
        _motionGrid.Rows.Add("10 positions");
        _motionGrid.Rows.Add("15 positions");
        _motionGrid.Rows.Add("All retained");
        _rawGrid.Rows.Add("Inclinometer pitch (deg)");
        _rawGrid.Rows.Add("Inclinometer roll (deg)");
        _rawGrid.Rows.Add("Inclinometer yaw (deg)");
        _rawGrid.Rows.Add("Accelerometer X (g)");
        _rawGrid.Rows.Add("Accelerometer Y (g)");
        _rawGrid.Rows.Add("Accelerometer Z (g)");
        _rawGrid.Rows.Add("Gyro X (deg/s)");
        _rawGrid.Rows.Add("Gyro Y (deg/s)");
        _rawGrid.Rows.Add("Gyro Z (deg/s)");
        _rawGrid.Rows.Add("Magnetometer X (uT)");
        _rawGrid.Rows.Add("Magnetometer Y (uT)");
        _rawGrid.Rows.Add("Magnetometer Z (uT)");
        _rawGrid.Rows.Add("Altimeter:");
        _rawValueColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _axisValueColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _rawGrid.DefaultCellStyle.NullValue = "---";

        foreach (DataGridViewRow row in _rawGrid.Rows)
        {
            row.Cells[1].ToolTipText = "Original internal device axes and units; unavailable sensors show ---.";
            row.Cells[2].ToolTipText = row.Index is >= 9 and <= 11
                ? "The current Bluetooth notification does not report magnetic field components."
                : "Original Bluetooth device axes and units; not fusion input or synchronized ground truth.";
        }

        _initialized = true;
        ApplyReadingFormats(_formats);
        ApplyRawImu(null);
        ResizeGrids();
        ApplySnapshot(GpsMotionSnapshot.Empty, false, double.NaN, double.NaN);
    }

    /// <summary>Applies the same user-selected precision as the telemetry panel.</summary>
    internal void ApplyReadingFormats(ReadingFormats formats)
    {
        _formats = formats.Normalized();
        _speedColumn.DefaultCellStyle.Format = $"N{_formats.SpeedDigits}";
        _courseColumn.DefaultCellStyle.Format = $"N{_formats.RestDigits}";
        _durationColumn.DefaultCellStyle.Format = $"N{_formats.RestDigits}";
        _rawValueColumn.DefaultCellStyle.Format = $"N{_formats.RestDigits}";
        _axisValueColumn.DefaultCellStyle.Format = $"N{_formats.RestDigits}";
        for (int row = 3; row < 6; row++)
        {
            _rawGrid.Rows[row].Cells[1].Style.Format = $"N{_formats.AccelerationDigits}";
            _rawGrid.Rows[row].Cells[2].Style.Format = $"N{_formats.AccelerationDigits}";
        }
    }

    /// <summary>Shows platform inclinometer angles and device-frame acceleration without fusion.</summary>
    internal void ApplyRawImu(RawImuPayload? imu)
    {
        SetRawValue(0, 1, imu?.AbsolutePitchDegrees ?? double.NaN);
        SetRawValue(1, 1, imu?.AbsoluteRollDegrees ?? double.NaN);
        SetRawValue(2, 1, imu?.AbsoluteYawDegrees ?? double.NaN);
        SetRawValue(3, 1, imu?.Acceleration.X ?? double.NaN);
        SetRawValue(4, 1, imu?.Acceleration.Y ?? double.NaN);
        SetRawValue(5, 1, imu?.Acceleration.Z ?? double.NaN);
        SetRawValue(6, 1, imu is { HasAngularRate: true } x ? x.AngularRate.X : double.NaN);
        SetRawValue(7, 1, imu is { HasAngularRate: true } y ? y.AngularRate.Y : double.NaN);
        SetRawValue(8, 1, imu is { HasAngularRate: true } z ? z.AngularRate.Z : double.NaN);
    }

    /// <summary>Shows independent Bluetooth readings beside the internal collection, never in its place.</summary>
    internal void ApplyBluetoothReadings(Wt901Reading? reading, bool connected)
    {
        _axisValueColumn.Visible = connected;
        SetRawValue(0, 2, reading?.Pitch ?? double.NaN);
        SetRawValue(1, 2, reading?.Roll ?? double.NaN);
        SetRawValue(2, 2, reading?.Yaw ?? double.NaN);
        SetRawValue(3, 2, reading?.AccelerationX ?? double.NaN);
        SetRawValue(4, 2, reading?.AccelerationY ?? double.NaN);
        SetRawValue(5, 2, reading?.AccelerationZ ?? double.NaN);
        SetRawValue(6, 2, reading?.AngularRateX ?? double.NaN);
        SetRawValue(7, 2, reading?.AngularRateY ?? double.NaN);
        SetRawValue(8, 2, reading?.AngularRateZ ?? double.NaN);
    }

    /// <summary>Shows live auxiliary readings; absent or unrecorded sensors remain unavailable.</summary>
    internal void ApplyAuxiliaryReadings(double magneticX, double magneticY, double magneticZ, double altitude)
    {
        SetRawValue(9, 1, magneticX);
        SetRawValue(10, 1, magneticY);
        SetRawValue(11, 1, magneticZ);
        SetRawValue(12, 1, altitude);
    }

    private void SetRawValue(int row, int column, double value)
        => _rawGrid.Rows[row].Cells[column].Value = double.IsFinite(value) ? value : null;

    /// <summary>Updates the comparison without replacing unavailable measurements with zeros.</summary>
    internal void ApplySnapshot(
        GpsMotionSnapshot snapshot,
        bool fresh,
        double fusionSpeedMetersPerSecond,
        double fusionCourseDegrees)
    {
        SetRow(0, fusionSpeedMetersPerSecond, fusionCourseDegrees, null, null);
        SetRow(
            1,
            fresh ? snapshot.ReceiverSpeedMetersPerSecond : double.NaN,
            fresh ? snapshot.ReceiverCourseDegrees : double.NaN,
            null,
            snapshot.Timestamp is null ? 0 : 1);
        SetWindow(2, snapshot.TwoPoint, fresh);
        SetWindow(3, snapshot.FourPoint, fresh);
        SetWindow(4, snapshot.TenPoint, fresh);
        SetWindow(5, snapshot.FifteenPoint, fresh);
        SetWindow(6, snapshot.FullHistory, fresh);
        _motionStatus.Text = string.Create(
            CultureInfo.CurrentCulture,
            $"{(fresh ? "GPS current" : "GPS unavailable / stale")} | {snapshot.PointCount}/{GpsMotionTracker.DefaultCapacity} fixes | {snapshot.RejectedFixCount:N0} rejected")
            + (snapshot.Diagnostic is { } diagnostic ? $" | Last event: {diagnostic}" : string.Empty);
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_initialized)
        {
            _motionGrid.Font = Font;
            _motionStatus.Font = Font;
            _explanation.Font = Font;
            _motionGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            _motionGrid.AutoResizeColumnHeadersHeight();
            _motionGrid.AutoResizeRows();
            _rawGrid.Font = Font;
            _rawGrid.AutoResizeColumns();
            _rawGrid.AutoResizeColumnHeadersHeight();
            _rawGrid.AutoResizeRows();
            ResizeGrids();
            PerformLayout();
        }
    }

    private void ResizeGrids()
    {
        _motionGrid.Height = _motionGrid.ColumnHeadersHeight
            + _motionGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible)
            + SystemInformation.HorizontalScrollBarHeight + 4;
        _rawGrid.Height = _rawGrid.ColumnHeadersHeight
            + _rawGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible)
            + SystemInformation.HorizontalScrollBarHeight + 4;
    }

    private void SetWindow(int row, GpsMotionWindow window, bool fresh)
        => SetRow(
            row,
            fresh ? window.SpeedMetersPerSecond : double.NaN,
            fresh ? window.CourseDegrees : double.NaN,
            window.PointCount >= 2 ? window.Duration.TotalSeconds : null,
            window.PointCount);

    private void SetRow(int row, double speed, double course, double? duration, int? count)
    {
        DataGridViewCellCollection cells = _motionGrid.Rows[row].Cells;
        cells[1].Value = double.IsFinite(speed) ? speed * 3.6d : null;
        cells[2].Value = double.IsFinite(course) ? course : null;
        cells[3].Value = duration;
        cells[4].Value = count;
    }
}
