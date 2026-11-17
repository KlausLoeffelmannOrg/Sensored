using System.Drawing.Drawing2D;

namespace Sensored;

/// <summary>Measures status text at the owner's DPI and confines capped captions to their items.</summary>
internal sealed class DashboardStatusRenderer : ToolStripProfessionalRenderer
{
    internal static int MeasureTextWidth(ToolStripStatusLabel label)
    {
        using Graphics graphics = label.Owner?.CreateGraphics() ?? Graphics.FromHwnd(IntPtr.Zero);
        int width = TextRenderer.MeasureText(graphics, label.Text, label.Font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
        int inset = (int)Math.Ceiling(4d * graphics.DpiX / 96d);

        return width + label.Padding.Horizontal + inset;
    }

    /// <inheritdoc/>
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item is not ToolStripStatusLabel)
        {
            base.OnRenderItemText(e);

            return;
        }

        Rectangle bounds = Rectangle.Intersect(e.Item.ContentRectangle,
            new Rectangle(Point.Empty, e.Item.Size));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        GraphicsState state = e.Graphics.Save();
        try
        {
            e.Graphics.SetClip(bounds, CombineMode.Intersect);
            TextFormatFlags flags = (e.TextFormat | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping)
                & ~(TextFormatFlags.NoClipping | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, bounds, e.TextColor, flags);
        }
        finally
        {
            e.Graphics.Restore(state);
        }
    }
}
