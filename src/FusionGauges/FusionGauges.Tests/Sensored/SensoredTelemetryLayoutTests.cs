using WarpToolkit.WinForms.DirectX.Controls;
using Sensored.TabViews;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.DirectX.Controls.Compositing;
using WarpToolkit.WinForms.DirectX.Controls.Elements;
using WarpToolkit.WinForms.DirectX.Controls.Rendering;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored.Tests;

/// <summary>Guards full-width dashboard text and independent, decimal-aligned raw diagnostics.</summary>
[Collection(nameof(SensoredUiTestCollection))]
public sealed class SensoredTelemetryLayoutTests
{
#if NET11_0_OR_GREATER
    [Fact]
    public void RouteSectionsUseOneColumnAndTheWholeAvailableWidth()
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using RouteSectionTabView view = new() { Size = new Size(1000, 600) };
            view.CreateControl();
            view.PerformLayout();
            GroupBox group = Assert.IsType<GroupBox>(Assert.Single(view.Controls.Cast<Control>()));
            RoadSectionClassificationGrid grid = Assert.Single(group.Controls.OfType<RoadSectionClassificationGrid>());
            Assert.Single(grid.Columns.Cast<DataGridViewColumn>());
            view.Append(RoadSectionClassificationEntry.Create(RoadSectionClassification.Straight));
            grid.PerformLayout();
            Assert.True(grid.Columns[0].Width >= grid.ClientSize.Width - 24);
            Assert.Single(grid.Rows.Cast<DataGridViewRow>());
        });
#endif

    [Theory]
    [InlineData(9f)]
    [InlineData(18f)]
    public void DebugConsoleUsesEqualWidthCellsAtTheInheritedFontSize(float points)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Font font = new("Segoe UI", points);
            using DebugTabView view = new() { Font = font, Size = new Size(1000, 600) };
            D2DConsole console = Assert.Single(view.Controls.OfType<D2DConsole>());
            Assert.Equal(points, console.Font.SizeInPoints);
            Size narrow = TextRenderer.MeasureText("iiiiiiiiii", console.Font, Size.Empty, TextFormatFlags.NoPadding);
            Size wide = TextRenderer.MeasureText("MMMMMMMMMM", console.Font, Size.Empty, TextFormatFlags.NoPadding);
            Assert.Equal(wide.Width, narrow.Width);
            Assert.Equal(DockStyle.Fill, console.Dock);
        });

    [Theory]
    [InlineData(9f)]
    [InlineData(18f)]
    public void RawDataKeepsGpsComparisonAndSeparatesInternalAndBluetoothCollections(float points)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Font font = new("Consolas", points);
            using GpsMotionTabView view = new() { Font = font, Size = new Size(1100, 900) };
            view.ApplyReadingFormats(new(3, 7, 1, 2));
            TableLayoutPanel layout = Assert.Single(view.Controls.OfType<TableLayoutPanel>());
            DataGridView raw = layout.Controls.OfType<DataGridView>().Single(grid => grid.Name == "_rawGrid");
            DataGridView motion = layout.Controls.OfType<DataGridView>().Single(grid => grid.Name == "_motionGrid");
            Assert.Equal(7, motion.Rows.Count);
            Assert.Equal(13, raw.Rows.Count);
            Assert.Equal("Internal Sensors", raw.Columns[1].HeaderText);
            Assert.Equal("Bluetooth Sensors", raw.Columns[2].HeaderText);
            Assert.True(raw.Margin.Top >= 20);
            Assert.Equal("Raw Data", view.AccessibleName);
            Assert.Null(raw.Rows[0].Cells[1].Value);
            RawImuPayload imu = new(new Vec3(1d, 2d, 3d), Vec3.Zero, false,
                double.NaN, 0d, 12d, -34d, 56d, DeviceYawSource.WindowsFusedOrientation);
            view.ApplyRawImu(imu);
            view.ApplyAuxiliaryReadings(4d, 5d, 6d, 7d);
            Assert.Equal(12d, raw.Rows[0].Cells[1].Value);
            Assert.Equal(-34d, raw.Rows[1].Cells[1].Value);
            Assert.Equal(56d, raw.Rows[2].Cells[1].Value);
            Assert.Equal(1d, raw.Rows[3].Cells[1].Value);
            Assert.Equal(2d, raw.Rows[4].Cells[1].Value);
            Assert.Equal(3d, raw.Rows[5].Cells[1].Value);
            Assert.All(raw.Rows.Cast<DataGridViewRow>().Skip(6).Take(3),
                row => Assert.Equal("---", row.Cells[1].FormattedValue));
            Assert.Equal(4d, raw.Rows[9].Cells[1].Value);
            Assert.Equal(5d, raw.Rows[10].Cells[1].Value);
            Assert.Equal(6d, raw.Rows[11].Cells[1].Value);
            Assert.Equal(7d, raw.Rows[12].Cells[1].Value);
            Wt901Reading bluetooth = new(DateTimeOffset.UtcNow, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            view.ApplyBluetoothReadings(bluetooth, connected: true);
            Assert.True(raw.Columns[2].Visible);
            Assert.Equal(15d, raw.Rows[0].Cells[2].Value);
            Assert.Equal(14d, raw.Rows[1].Cells[2].Value);
            Assert.Equal(16d, raw.Rows[2].Cells[2].Value);
            Assert.Equal(8d, raw.Rows[3].Cells[2].Value);
            Assert.Equal(11d, raw.Rows[6].Cells[2].Value);
            Assert.Null(raw.Rows[9].Cells[2].Value);
            Assert.Equal(12d, raw.Rows[0].Cells[1].Value);
            Assert.Equal("N2", raw.Columns[1].DefaultCellStyle.Format);
            Assert.Equal("N3", raw.Rows[3].Cells[1].Style.Format);
            Assert.Equal("N3", raw.Rows[3].Cells[2].Style.Format);
            Assert.Equal(DataGridViewContentAlignment.MiddleRight, raw.Columns[1].DefaultCellStyle.Alignment);
            Assert.Equal(DataGridViewContentAlignment.MiddleRight, raw.Columns[2].DefaultCellStyle.Alignment);
            Assert.True(raw.Height >= raw.Rows.GetRowsHeight(DataGridViewElementStates.Visible));
            view.ApplyRawImu(null);
            view.ApplyAuxiliaryReadings(double.NaN, double.NaN, double.NaN, double.NaN);
            view.ApplyBluetoothReadings(null, connected: true);
            Assert.All(raw.Rows.Cast<DataGridViewRow>(), row => Assert.Null(row.Cells[1].Value));
            Assert.All(raw.Rows.Cast<DataGridViewRow>(), row => Assert.Null(row.Cells[2].Value));
            Assert.True(raw.Columns[2].Visible);
            view.ApplyRawImu(imu with { HasAngularRate = true, AngularRate = new(17, 18, 19) });
            Assert.Equal(17d, raw.Rows[6].Cells[1].Value);
            Assert.Equal(18d, raw.Rows[7].Cells[1].Value);
            Assert.Equal(19d, raw.Rows[8].Cells[1].Value);
            view.ApplyBluetoothReadings(null, connected: false);
            Assert.False(raw.Columns[2].Visible);
        });
}
