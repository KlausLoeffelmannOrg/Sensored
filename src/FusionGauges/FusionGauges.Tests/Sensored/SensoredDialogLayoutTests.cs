using System.Reflection;
using Sensored;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored.Tests;

/// <summary>Checks content-driven dialog sizing, font reuse, and equal-height action rows.</summary>
[Collection(nameof(SensoredUiTestCollection))]
public sealed class SensoredDialogLayoutTests
{
    [Theory]
    [InlineData(9f)]
    [InlineData(12f)]
    [InlineData(18f)]
    [InlineData(24f)]
    public void CleanupFitsContentAndResizesItsVisibleActionRow(float points)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, points);
            using CleanupProgressForm dialog = new("Finishing sensor cleanup. Please wait.", font);
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(-20000, -20000);
            dialog.TopMost = false;
            dialog.Show();
            Assert.Equal(points, dialog.Font.SizeInPoints);
            Assert.True(dialog.AutoSize);
            Assert.Equal(AutoSizeMode.GrowAndShrink, dialog.AutoSizeMode);
            TableLayoutPanel layout = Assert.IsType<TableLayoutPanel>(Assert.Single(dialog.Controls.Cast<Control>()));
            Assert.True(layout.AutoSize);
            Assert.Equal(AutoSizeMode.GrowAndShrink, layout.AutoSizeMode);
            Assert.All(layout.RowStyles.Cast<RowStyle>(), row => Assert.Equal(SizeType.AutoSize, row.SizeType));
            TableLayoutPanel buttons = Assert.Single(layout.Controls.OfType<TableLayoutPanel>());

            foreach (var (running, canRetry, count) in new[] { (true, true, 2), (false, true, 3), (false, false, 2) })
            {
                typeof(CleanupProgressForm).GetMethod("ShowActions", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, [running, canRetry]);
                dialog.PerformLayout();
                Assert.True(buttons.AutoSize);
                Assert.Equal(AutoSizeMode.GrowAndShrink, buttons.AutoSizeMode);
                Assert.Equal(count, buttons.ColumnCount);
                Assert.All(buttons.ColumnStyles.Cast<ColumnStyle>(), column =>
                {
                    Assert.Equal(SizeType.Percent, column.SizeType);
                    Assert.InRange(Math.Abs(column.Width - (100f / count)), 0f, 0.0001f);
                });
                Assert.Equal(SizeType.AutoSize, buttons.RowStyles[0].SizeType);
                Button[] actions = buttons.Controls.OfType<Button>().Where(button => button.Visible).ToArray();
                Assert.Equal(count, actions.Length);
                Assert.All(actions, button =>
                {
                    Assert.Equal(points, button.Font.SizeInPoints);
                    Assert.True(button.AutoSize);
                    Assert.Equal(AutoSizeMode.GrowAndShrink, button.AutoSizeMode);
                    Assert.Equal(AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, button.Anchor);
                    Assert.Equal(actions[0].Height, button.Height);
                    Assert.InRange(Math.Abs(actions[0].Width - button.Width), 0, 1);
                    Assert.True(button.Height >= button.GetPreferredSize(Size.Empty).Height);
                });
                Assert.True(dialog.ClientRectangle.Contains(layout.Bounds));
                Assert.All(layout.Controls.Cast<Control>(), control =>
                    Assert.True(layout.ClientRectangle.Contains(control.Bounds),
                        $"{control.Name} {control.Bounds} exceeds {layout.ClientRectangle}; dialog {dialog.Bounds}."));
            }

            Button first = buttons.Controls.OfType<Button>().First(button => button.Visible);
            int shortHeight = first.Height;
            first.Text = DialogButtonText.WrapCaption("Return to the application after sensor cleanup has failed");
            dialog.PerformLayout();
            Assert.True(first.Height > shortHeight);
            Assert.All(buttons.Controls.OfType<Button>().Where(button => button.Visible),
                button => Assert.Equal(first.Height, button.Height));
            first.Text = "Return to app";
            dialog.PerformLayout();
            Assert.Equal(shortHeight, first.Height);

            typeof(CleanupProgressForm).GetMethod("ShowFailure", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(dialog, [new IOException(new string('x', 160)), false]);
            dialog.PerformLayout();
            Assert.All(layout.Controls.Cast<Control>(), control =>
                Assert.True(layout.ClientRectangle.Contains(control.Bounds)));
        });
}
