using WarpToolkit.ComponentModel;
using WarpToolkit.WinForms.Extensions.UI;

namespace Sensored;

/// <summary>
///  Restores dialog bounds within an available monitor or centers a safe default over the owner.
/// </summary>
internal static class DialogBoundsPlacement
{
    /// <summary>
    ///  Restores a saved rectangle, or places the dialog over its owner with a screen-safe size.
    /// </summary>
    internal static void Restore(
        Form form,
        IUserSettingsService? settings,
        string key,
        bool useRawSensorDefaultSize = false)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Size defaultSize = form.Size;
        bool restored = settings?.TryApplyFormBounds(form, key) ?? false;
        Screen? restoredScreen = restored
            ? Screen.AllScreens
                .Where(candidate => candidate.WorkingArea.IntersectsWith(form.Bounds))
                .MaxBy(candidate => VisibleArea(candidate.WorkingArea, form.Bounds))
            : null;
        restored = restoredScreen is not null;
        Screen screen = restoredScreen ?? Screen.FromControl(form.Owner ?? form);
        Rectangle workingArea = screen.WorkingArea;
        Size desiredSize = useRawSensorDefaultSize && !restored
            ? new Size(
                (int)Math.Round(workingArea.Width * 0.6d),
                (int)Math.Round(workingArea.Height * 0.7d))
            : restored ? form.Size : defaultSize;
        Size size = new(
            Math.Min(workingArea.Width, Math.Max(form.MinimumSize.Width, desiredSize.Width)),
            Math.Min(workingArea.Height, Math.Max(form.MinimumSize.Height, desiredSize.Height)));

        Rectangle reference = restored
            ? form.Bounds
            : form.Owner is { } owner && owner.Bounds.IntersectsWith(workingArea)
                ? owner.Bounds
                : workingArea;
        int x = restored
            ? reference.Left
            : reference.Left + (reference.Width - size.Width) / 2;
        int y = restored
            ? reference.Top
            : reference.Top + (reference.Height - size.Height) / 2;

        form.StartPosition = FormStartPosition.Manual;
        form.Bounds = new Rectangle(
            Math.Clamp(x, workingArea.Left, workingArea.Right - size.Width),
            Math.Clamp(y, workingArea.Top, workingArea.Bottom - size.Height),
            size.Width,
            size.Height);
    }

    /// <summary>
    ///  Saves the restorable location and size without forcing an immediate settings flush.
    /// </summary>
    internal static void Save(Form form, IUserSettingsService? settings, string key)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        settings?.SaveFormBounds(form, key);
    }

    private static long VisibleArea(Rectangle workingArea, Rectangle bounds)
    {
        Rectangle intersection = Rectangle.Intersect(workingArea, bounds);

        return (long)intersection.Width * intersection.Height;
    }
}
