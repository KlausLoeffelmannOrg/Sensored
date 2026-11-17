using System.Diagnostics;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.DirectX.D2D;
using DW = WarpToolkit.WinForms.DirectWrite;

namespace Sensored;

/// <summary>Shows a non-activating, per-pixel-alpha notification or short question over the map.</summary>
/// <remarks>All content is Direct2D; no layered-window opacity or classic child HWNDs are used.</remarks>
internal sealed partial class D2DPopup : D2DForm
{
    private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromSeconds(1);
    private const int AnimationIntervalMilliseconds = 16;
    private const float BackgroundOpacity = 0.7f;
    private readonly DW.DirectWriteFactory _textFactory = DW.DirectWriteFactory.Create();
    private DW.TextLayout? _textLayout;
    private readonly List<DW.TextLayout> _answerLayouts = [];
    private RectangleF[] _answerBounds = [];
    private string[] _answers = [];
    private Action<int>? _onAnswer;
    private int _focusedAnswer = -1;
    private float _contentHeight;
    private Control? _anchor;
    private long _animationStarted;
    private float _slideProgress;
    private float _fadeOpacity;
    private float _contentPadding;
    private float _scrollOffset;
    private int _layoutDpi;

    /// <summary>Initializes an owned notification surface without stealing keyboard focus.</summary>
    internal D2DPopup()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        InitializeComponent();
        Render += OnRender;
        Disposed += OnPopupDisposed;
    }

    /// <inheritdoc/>
    protected override bool DefaultUseDirectCompositionExclusiveRendering => true;

    /// <inheritdoc/>
    protected override bool ShowWithoutActivation => true;

    /// <inheritdoc/>
    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x00000080;
            const int WsExNoActivate = 0x08000000;
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;

            return parameters;
        }
    }

    /// <summary>Replaces the current notification and restarts its slide, hold and fade sequence.</summary>
    internal void ShowNotification(Form owner, Control anchor, string message)
        => ShowContent(owner, anchor, message, [], onAnswer: null);

    /// <summary>Shows a short question until an explicit answer or dismissal, without a timed default.</summary>
    /// <param name="owner">The owning form.</param>
    /// <param name="anchor">The map area in which the popup stays.</param>
    /// <param name="message">The question to display.</param>
    /// <param name="answers">The caller's answer captions, in display order.</param>
    /// <param name="onAnswer">Receives the selected zero-based index; dismissal does not invoke it.</param>
    internal void ShowQuestion(Form owner, Control anchor, string message,
        IReadOnlyList<string> answers, Action<int> onAnswer)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(onAnswer);
        ArgumentOutOfRangeException.ThrowIfZero(answers.Count);
        foreach (string answer in answers)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        }

        ShowContent(owner, anchor, message, [.. answers], onAnswer);
    }

    /// <summary>Gets whether an unanswered question is currently visible.</summary>
    internal bool IsQuestion => Visible && _onAnswer is not null;

    private void ShowContent(Form owner, Control anchor, string message,
        string[] answers, Action<int>? onAnswer)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _animationTimer.Stop();
        _anchor = anchor;
        Owner = owner;
        Font = owner.Font;
        Text = message;
        AccessibleDescription = onAnswer is null
            ? message
            : $"{message}\n{string.Join("; ", answers)}\nTab selects an answer; Enter confirms; Escape dismisses without answering.";
        AccessibleRole = onAnswer is null ? AccessibleRole.Alert : AccessibleRole.Dialog;
        _answers = answers;
        _onAnswer = onAnswer;
        _focusedAnswer = -1;
        _textLayout?.Dispose();
        _textLayout = null;
        _scrollOffset = 0;
        _slideProgress = 0;
        _fadeOpacity = 1;
        if (!UpdatePlacement())
        {
            Dismiss();

            return;
        }

        _animationStarted = Stopwatch.GetTimestamp();
        if (!Visible)
        {
            Show(owner);
        }

        _animationTimer.Interval = AnimationIntervalMilliseconds;
        _animationTimer.Start();
        Invalidate();
    }

    /// <summary>Hides the notification without discarding the owner's last message.</summary>
    internal void Dismiss()
    {
        _animationTimer.Stop();
        _fadeOpacity = 0;
        _onAnswer = null;
        Hide();
    }

    /// <summary>Keeps the top-level surface inside the map after layout or window movement.</summary>
    internal void Reposition()
    {
        if (Visible && !UpdatePlacement())
        {
            Dismiss();
        }
    }

    private bool UpdatePlacement()
    {
        if (_anchor is not { Visible: true, IsDisposed: false } anchor
            || Owner is not { Visible: true, IsDisposed: false } owner
            || owner.WindowState == FormWindowState.Minimized)
        {
            return false;
        }

        Rectangle available = Rectangle.Intersect(
            anchor.RectangleToScreen(anchor.ClientRectangle),
            owner.RectangleToScreen(owner.ClientRectangle));
        available.Intersect(Screen.FromControl(anchor).Bounds);
        float scale = anchor.DeviceDpi / 96f;
        int inset = Math.Min((int)Math.Ceiling(12 * scale), Math.Min(available.Width, available.Height) / 8);
        available.Inflate(-inset, -inset);
        if (available.Width <= 0 || available.Height <= 0)
        {
            return false;
        }

        int width = Math.Min((int)Math.Ceiling(640 * scale), available.Width);
        _contentPadding = Math.Min(16 * scale, Math.Min(width, available.Height) / 4f);
        float textWidth = Math.Max(1, width - 2 * _contentPadding);
        bool fontChanged = !Font.Equals(owner.Font);
        Font = owner.Font;
        if (_textLayout is null || Math.Abs(_textLayout.MaxWidth - textWidth) > 0.5f
            || _layoutDpi != anchor.DeviceDpi || fontChanged)
        {
            using DW.TextFormat format = _textFactory.CreateTextFormat(
                Font.FontFamily.Name,
                Font.SizeInPoints * anchor.DeviceDpi / 72f,
                Font.Bold ? DW.FontWeight.Bold : DW.FontWeight.Normal,
                Font.Italic ? DW.FontStyle.Italic : DW.FontStyle.Normal);
            format.WordWrapping = DW.WordWrapping.Wrap;
            _textLayout?.Dispose();
            _textLayout = _textFactory.CreateTextLayout(Text, format, textWidth, float.MaxValue);
            LayoutAnswers(format, width, scale);
            _layoutDpi = anchor.DeviceDpi;
        }

        int height = Math.Min(available.Height,
            (int)Math.Ceiling(_contentHeight + 2 * _contentPadding));
        Bounds = new Rectangle(available.Left + (available.Width - width) / 2, available.Top, width, height);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, MaximumScrollOffset);
        Invalidate();

        return true;
    }

    private float MaximumScrollOffset
        => Math.Max(0, _contentHeight - ClientSize.Height + 2 * _contentPadding);

    private void LayoutAnswers(DW.TextFormat format, int width, float scale)
    {
        foreach (DW.TextLayout layout in _answerLayouts)
        {
            layout.Dispose();
        }

        _answerLayouts.Clear();
        _answerBounds = new RectangleF[_answers.Length];
        _contentHeight = _textLayout!.GetMetrics().Height;
        float gap = 8 * scale;
        int columns = width >= 400 * scale ? 2 : 1;
        float buttonWidth = (width - 2 * _contentPadding - gap * (columns - 1)) / columns;
        float top = _contentPadding + _contentHeight + gap;
        for (int first = 0; first < _answers.Length; first += columns)
        {
            float rowHeight = 48 * scale;
            int end = Math.Min(first + columns, _answers.Length);
            for (int index = first; index < end; index++)
            {
                DW.TextLayout layout = _textFactory.CreateTextLayout(
                    _answers[index], format, Math.Max(1, buttonWidth - 2 * gap), float.MaxValue);
                _answerLayouts.Add(layout);
                rowHeight = Math.Max(rowHeight, layout.GetMetrics().Height + 2 * gap);
            }

            for (int index = first; index < end; index++)
            {
                _answerBounds[index] = new(
                    _contentPadding + (index - first) * (buttonWidth + gap), top, buttonWidth, rowHeight);
            }

            top += rowHeight + gap;
            _contentHeight = top - gap - _contentPadding;
        }
    }

    private void OnAnimationTick(object? sender, EventArgs e)
        => UpdateAnimation(Stopwatch.GetElapsedTime(_animationStarted));

    /// <summary>Advances the animation using elapsed time rather than timer tick counts.</summary>
    internal void UpdateAnimation(TimeSpan elapsed)
    {
        if (_onAnswer is null && elapsed >= SlideDuration + HoldDuration + FadeDuration)
        {
            Dismiss();

            return;
        }

        float slide = (float)Math.Clamp(elapsed.TotalMilliseconds / SlideDuration.TotalMilliseconds, 0, 1);
        _slideProgress = 1 - MathF.Pow(1 - slide, 3);
        if (_onAnswer is not null)
        {
            _fadeOpacity = 1;
            if (elapsed >= SlideDuration)
            {
                _animationTimer.Stop();
            }

            Invalidate();

            return;
        }

        _fadeOpacity = 1 - (float)Math.Clamp(
            (elapsed - SlideDuration - HoldDuration).TotalMilliseconds / FadeDuration.TotalMilliseconds, 0, 1);
        bool holding = elapsed >= SlideDuration && elapsed < SlideDuration + HoldDuration;
        _animationTimer.Interval = holding
            ? Math.Max(1, (int)Math.Ceiling((SlideDuration + HoldDuration - elapsed).TotalMilliseconds))
            : AnimationIntervalMilliseconds;
        Invalidate();
    }

    private void OnRender(object? sender, D2DRenderEventArgs e)
    {
        ID2DGraphics graphics = e.Graphics;
        graphics.Clear(Color.FromArgb(0, 0, 0, 0));
        float offset = -ClientSize.Height * (1 - _slideProgress);
        Color background = Application.IsDarkModeEnabled ? Color.Black : Color.White;
        Color foreground = Application.IsDarkModeEnabled ? Color.White : Color.Black;
        graphics.FillRectangle(
            Color.FromArgb((int)MathF.Round(255 * BackgroundOpacity * _fadeOpacity), background),
            new RectangleF(0, offset, ClientSize.Width, ClientSize.Height));
        if (_textLayout is not null)
        {
            graphics.SetClip(new RectangleF(
                _contentPadding, offset + _contentPadding,
                Math.Max(0, ClientSize.Width - 2 * _contentPadding),
                Math.Max(0, ClientSize.Height - 2 * _contentPadding)));
            try
            {
                graphics.DrawTextLayout(
                    _textLayout,
                    new PointF(_contentPadding, offset + _contentPadding - _scrollOffset),
                    Color.FromArgb((int)MathF.Round(255 * _fadeOpacity), foreground),
                    DW.ColorFontMode.Disabled);
                for (int index = 0; index < _answerLayouts.Count; index++)
                {
                    RectangleF bounds = _answerBounds[index];
                    bounds.Offset(0, offset - _scrollOffset);
                    graphics.FillRoundedRectangle(
                        Color.FromArgb((int)MathF.Round(48 * _fadeOpacity), foreground),
                        bounds, new SizeF(6, 6));
                    if (index == _focusedAnswer)
                    {
                        graphics.DrawRectangle(
                            Color.FromArgb((int)MathF.Round(255 * _fadeOpacity), foreground), 2, bounds);
                    }

                    float padding = (bounds.Width - _answerLayouts[index].MaxWidth) / 2;
                    graphics.DrawTextLayout(_answerLayouts[index],
                        new PointF(bounds.Left + padding,
                            bounds.Top + (bounds.Height - _answerLayouts[index].GetMetrics().Height) / 2),
                        Color.FromArgb((int)MathF.Round(255 * _fadeOpacity), foreground),
                        DW.ColorFontMode.Disabled);
                }
            }
            finally
            {
                graphics.ResetClip();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_onAnswer is null)
        {
            Dismiss();

            return;
        }

        if (e.Button != MouseButtons.Left || _slideProgress < 1
            || e.X < _contentPadding || e.X >= ClientSize.Width - _contentPadding
            || e.Y < _contentPadding || e.Y >= ClientSize.Height - _contentPadding)
        {
            return;
        }

        PointF point = new(e.X, e.Y + _scrollOffset);
        for (int index = 0; index < _answerBounds.Length; index++)
        {
            if (_answerBounds[index].Contains(point))
            {
                SelectAnswer(index);

                return;
            }
        }
    }

    /// <summary>Routes explicit question navigation from the owner without activating the popup.</summary>
    internal bool ProcessQuestionKey(Keys keyData)
    {
        if (!IsQuestion)
        {
            return false;
        }

        if (keyData == Keys.Escape)
        {
            Dismiss();

            return true;
        }

        if (keyData is Keys.Tab or (Keys.Shift | Keys.Tab)
            or Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            bool previous = keyData is (Keys.Shift | Keys.Tab) or Keys.Left or Keys.Up;
            _focusedAnswer = _focusedAnswer < 0
                ? previous ? _answers.Length - 1 : 0
                : (_focusedAnswer + (previous ? _answers.Length - 1 : 1)) % _answers.Length;
            RectangleF bounds = _answerBounds[_focusedAnswer];
            _scrollOffset = Math.Clamp(bounds.Bottom - ClientSize.Height + _contentPadding, 0, MaximumScrollOffset);
            Invalidate();

            return true;
        }

        if (keyData is Keys.Enter or Keys.Space && _focusedAnswer >= 0)
        {
            SelectAnswer(_focusedAnswer);

            return true;
        }

        return false;
    }

    private void SelectAnswer(int index)
    {
        Action<int>? onAnswer = _onAnswer;
        Dismiss();
        onAnswer?.Invoke(index);
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _scrollOffset = Math.Clamp(_scrollOffset - e.Delta / 120f * Font.Height * 3, 0, MaximumScrollOffset);
        Invalidate();
    }

    private void OnPopupDisposed(object? sender, EventArgs e)
    {
        _textLayout?.Dispose();
        foreach (DW.TextLayout layout in _answerLayouts)
        {
            layout.Dispose();
        }

        _textFactory.Dispose();
    }
}
