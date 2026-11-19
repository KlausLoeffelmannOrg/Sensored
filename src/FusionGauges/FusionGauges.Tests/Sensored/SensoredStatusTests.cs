using Sensored;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;

namespace Sensored.Tests;

/// <summary>Verifies the compact status contract and explicit unavailable raw readings.</summary>
[Collection(nameof(SensoredUiTestCollection))]
public sealed class SensoredStatusTests
{
    [Theory]
    [InlineData(10f)]
    [InlineData(18f)]
    public void CappedStatusTextCannotPaintIntoItsNeighbor(float points)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Font font = new("Segoe UI", points);
            using StatusStrip strip = new() { Font = font };
            using ToolStripStatusLabel label = new()
            {
                AutoSize = false,
                Size = new Size(100, 50),
                Text = "RAW: P+12.34 R-56.78 Y+90.12 KM/H 123",
                TextAlign = ContentAlignment.MiddleLeft
            };
            strip.Items.Add(label);
            strip.CreateControl();
            Assert.True(DashboardStatusRenderer.MeasureTextWidth(label) > label.Width);
            using Bitmap bitmap = new(800, 80);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Magenta);
            DashboardStatusRenderer renderer = new();
            renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(
                graphics, label, label.Text, new Rectangle(0, 0, 800, 50), Color.White,
                font, TextFormatFlags.Left | TextFormatFlags.NoClipping));
            for (int x = label.Width; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
                }
            }
        });

    [Theory]
    [InlineData(SensorServiceState.Stopped, "Stopped")]
    [InlineData(SensorServiceState.Starting, "Starting up...")]
    [InlineData(SensorServiceState.Running, "Running at 100.0 HZ (GPS: 5.0 HZ)")]
    [InlineData(SensorServiceState.Stopping, "Stopping...(sources [2])")]
    public void LifecycleHasStableMeasuredRateAndCleanupFormatting(SensorServiceState state, string expected)
        => Assert.Equal(expected, DashboardStatusText.Session(state, new SensorRateSnapshot(100, 5), "sources", 2));

    [Fact]
    public void RawAttitudeHasSignsAndSpeedUsesKilometersPerHour()
    {
        RawImuPayload imu = new(default, default, default, default, default, -1.25, 2.5, -0.001, default);
        Assert.Equal("RAW: P-1.25 R+2.50 Y+0.00 KM/H 036", DashboardStatusText.Raw(imu, 10));
        Assert.Equal("RAW: P-- R-- Y-- KM/H ---", DashboardStatusText.Raw(null, double.NaN));
        Assert.Equal("RAW: P-- R-- Y-- KM/H ---", DashboardStatusText.Raw(null, -1));
    }

    [Fact]
    public void CleanupDisplaysTheActualWorkInsteadOfAFormatPlaceholder()
        => Assert.Equal("Stopping...(Restoring the GPS receiver. [3])",
            DashboardStatusText.Session(SensorServiceState.Stopping, default,
                "Restoring the GPS receiver.", 3));
}
