namespace Sensored;

/// <summary>
///  Presents the consolidated Sources menu and the spherical-horizon layout.
/// </summary>
public partial class MainForm
{
    /// <summary>
    ///  Applies Bluetooth availability reported by sensor acquisition.
    /// </summary>
    /// <param name="available">Whether a fresh Bluetooth reference can be selected.</param>
    /// <param name="status">The current Bluetooth search or connection status.</param>
    private void UpdateSensorSourcesUi(bool available, string status)
    {
        bool commandsEnabled = !_shutdownStarted && !_sensorSelectionPending && !_obdReplayActive;
        _bluetoothSensorsMenuItem.Enabled = available && commandsEnabled;
        _bluetoothSensorsMenuItem.ToolTipText = status;
        _bluetoothSensorsMenuItem.AccessibleDescription = status;
        _internalSensorsMenuItem.Checked = _options.SensorSource == SensorDisplaySource.Internal;
        _bluetoothSensorsMenuItem.Checked = _options.SensorSource == SensorDisplaySource.Bluetooth;
        _internalSensorsMenuItem.Enabled = commandsEnabled;
    }

    /// <summary>
    ///  Places engine instruments below speed and acceleration below the horizon.
    /// </summary>
    /// <param name="obdInstrumentsVisible">Whether OBD instruments have data or are forced on.</param>
    /// <remarks>
    ///  Map column-span hides the gauge panel. OBD stays in the map split in that case so disabling
    ///  or expanding the map cannot leave instruments in a hidden gauge cell.
    /// </remarks>
    private void ApplySourceDisplayLayout(bool obdInstrumentsVisible)
    {
        bool sphere = _options.UseSphericalHorizon;
        _artificialHorizon.UseSphericalDisplay = sphere;
        _kiosk.AlwaysOn = _options.AlwaysOn;

        bool gaugesVisible = !_mapEnabled || _mapColumnSpan == 1;
        bool obdInGaugeCell = gaugesVisible;
        if (obdInGaugeCell)
        {
            if (!ReferenceEquals(_obdView.Parent, _gaugeLayout))
            {
                _obdView.Parent?.Controls.Remove(_obdView);
                _gaugeLayout.Controls.Add(_obdView, 0, 1);
            }

            _obdView.BringToFront();
            _compassGauge.Visible = false;
            SetMapObdColumn(0F);
            _obdView.Visible = obdInstrumentsVisible;
        }
        else
        {
            if (!ReferenceEquals(_obdView.Parent, _mapLayout))
            {
                _obdView.Parent?.Controls.Remove(_obdView);
                _mapLayout.Controls.Add(_obdView, 0, 0);
            }

            _compassGauge.Visible = !sphere;
            SetMapObdColumn(obdInstrumentsVisible ? 50F : 0F);
            _obdView.Visible = obdInstrumentsVisible && _mapEnabled;
        }
    }

    private void SetMapObdColumn(float obdWidth)
    {
        float mapWidth = 100F - obdWidth;
        if (_mapLayout.ColumnStyles[0].Width == obdWidth
            && _mapLayout.ColumnStyles[1].Width == mapWidth)
        {
            return;
        }

        _mapLayout.SuspendLayout();
        _mapLayout.ColumnStyles[0].Width = obdWidth;
        _mapLayout.ColumnStyles[1].Width = mapWidth;
        _mapLayout.ResumeLayout(performLayout: true);
    }
}
