using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ChatClient.Avalonia.Controls;

public class DelayedPressButton : ContentControl
{
    private const string delayPressed = ":delayPressed";
    private const double ScrollThreshold = 4; // 移动多少像素算作滚动

    public static readonly RoutedEvent<RoutedEventArgs> PressOrLongPressEvent =
        RoutedEvent.Register<DelayedPressButton, RoutedEventArgs>(
            nameof(PressOrLongPress),
            RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> PressOrLongPress
    {
        add => AddHandler(PressOrLongPressEvent, value);
        remove => RemoveHandler(PressOrLongPressEvent, value);
    }

    private DispatcherTimer _pressDelayTimer;
    private Point _pointerPressedPosition;

    private bool _isScrolling;
    private bool _isPressed;
    private bool _keepPressed;

    public DelayedPressButton()
    {
        _pressDelayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _pressDelayTimer.Tick += OnPressDelayTimerTick;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPressed = true;
        _pointerPressedPosition = e.GetCurrentPoint(TopLevel.GetTopLevel(this)).Position;
        _pressDelayTimer.Start();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (!_keepPressed)
            PseudoClasses.Set(delayPressed, false);
        _isPressed = false;
        _pressDelayTimer.Stop();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (!_keepPressed)
            PseudoClasses.Set(delayPressed, false);
        _isPressed = false;
        _pressDelayTimer.Stop();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isPressed)
            return;

        var currentPosition = e.GetCurrentPoint(TopLevel.GetTopLevel(this)).Position;
        var delta = currentPosition - _pointerPressedPosition;

        // 如果移动距离超过阈值，认为是滚动而非点击
        if (Math.Abs(delta.Y) > ScrollThreshold || Math.Abs(delta.X) > ScrollThreshold)
        {
            _isScrolling = true;
            if (_pressDelayTimer.IsEnabled)
                _pressDelayTimer.Stop();
            PseudoClasses.Set(delayPressed, false);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isPressed) return;
        _isPressed = false;
        _pressDelayTimer.Stop();

        if (!_isScrolling)
        {
            RaiseEvent(new RoutedEventArgs(PressOrLongPressEvent, this));

            // 如果是点击，手动触发Pressed状态
            _keepPressed = true;
            PseudoClasses.Set(delayPressed, true);

            // 延时100ms后移除Pressed状态
            DispatcherTimer delayRemoveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            delayRemoveTimer.Tick += (s, e) =>
            {
                delayRemoveTimer.Stop();
                Dispatcher.UIThread.Post(() => PseudoClasses.Set(delayPressed, false), DispatcherPriority.Background);
                _keepPressed = false;
            };
            delayRemoveTimer.Start();
        }
        else
            PseudoClasses.Set(delayPressed, false);

        _isScrolling = false;
    }

    private void OnPressDelayTimerTick(object sender, EventArgs e)
    {
        _pressDelayTimer.Stop();
        if (!_isScrolling)
            PseudoClasses.Set(delayPressed, true);
    }
}