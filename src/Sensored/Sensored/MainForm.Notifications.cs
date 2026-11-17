namespace Sensored;

/// <summary>Retains the last meaningful status change independently of the notification's visibility.</summary>
public partial class MainForm
{
    private D2DPopup? _statusNotification;
    private string? _lastStatusNotification;

    private void NotifyStatusChange(string message)
    {
        if (_shutdownStarted || IsDisposed || Disposing || message == _lastStatusNotification)
        {
            return;
        }

        _lastStatusNotification = message;
        ShowLastStatusNotification();
    }

    private void ShowLastStatusNotification()
    {
        if (_shutdownStarted || !Visible || !_routeMap.Visible
            || _statusNotification is { IsQuestion: true } || _lastStatusNotification is not { } message)
        {
            return;
        }

        _statusNotification ??= new D2DPopup();
        _statusNotification.ShowNotification(this, _routeMap.MapControl, message);
    }

    private void OnMapNotificationDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowLastStatusNotification();
        }
    }

    private void OnNotificationMapLayout(object? sender, LayoutEventArgs e)
        => _statusNotification?.Reposition();

    private void OnNotificationMapBoundsChanged(object? sender, EventArgs e)
        => _statusNotification?.Reposition();

    private void OnNotificationMapVisibilityChanged(object? sender, EventArgs e)
    {
        if (!_routeMap.Visible)
        {
            _statusNotification?.Dismiss();
        }
    }
}
