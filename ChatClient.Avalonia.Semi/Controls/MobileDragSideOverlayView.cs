using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace ChatClient.Avalonia.Semi.Controls;

public class MobileDragSideOverlayView : ContentControl
{
    public static readonly StyledProperty<object?> SidePanelProperty =
        AvaloniaProperty.Register<MobileDragSideOverlayView, object?>(
            "SidePanel");

    public object? SidePanel
    {
        get => GetValue(SidePanelProperty);
        set => SetValue(SidePanelProperty, value);
    }

    public static readonly StyledProperty<bool> IsPanelOpenedProperty =
        AvaloniaProperty.Register<MobileDragSideOverlayView, bool>(
            "IsPanelOpened");

    public bool IsPanelOpened
    {
        get => GetValue(IsPanelOpenedProperty);
        set => SetValue(IsPanelOpenedProperty, value);
    }

    public static readonly StyledProperty<double> CurrentPosXProperty =
        AvaloniaProperty.Register<MobileDragSideOverlayView, double>(
            "CurrentPosX");

    public double CurrentPosX
    {
        get => GetValue(CurrentPosXProperty);
        set => SetValue(CurrentPosXProperty, value);
    }

    private ContentPresenter _mainRegionContentControl = null!;
    private ContentPresenter _leftRegionContentControl = null!;
    private Border _maskBorder = null!;

    private ScaleTransform _mainRegionScaleTransform = null!;
    private TranslateTransform _leftRegionTranslateTransform = null!;

    private Transitions _maskTransitions = null!;
    private Transitions _mainTransitions = null!;
    private Transitions _leftTransitions = null!;
    private Transitions _cornerTransitions = null!;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _mainRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter")!;
        _leftRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_LeftContent")!;
        _maskBorder = e.NameScope.Find<Border>("PART_Mask")!;

        _cornerTransitions = _mainRegionContentControl.Transitions!;
        _mainRegionContentControl.Transitions = null;

        _mainRegionScaleTransform = _mainRegionContentControl.RenderTransform as ScaleTransform;
        _mainTransitions = _mainRegionScaleTransform!.Transitions;
        _mainRegionScaleTransform.Transitions = null;

        _leftRegionTranslateTransform = _leftRegionContentControl.RenderTransform as TranslateTransform;
        _leftTransitions = _leftRegionTranslateTransform!.Transitions;
        _leftRegionTranslateTransform.Transitions = null;

        _maskTransitions = _maskBorder.Transitions!;
        _maskBorder.Transitions = null;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        _leftRegionTranslateTransform.X = -Bounds.Width;

        base.OnLoaded(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentPosXProperty)
        {
            _mainRegionContentControl.CornerRadius = new CornerRadius(CurrentPosX / Bounds.Width * 30);

            _mainRegionScaleTransform.ScaleX = 1 - CurrentPosX / Bounds.Width * 0.1;
            _mainRegionScaleTransform.ScaleY = 1 - CurrentPosX / Bounds.Width * 0.1;

            _leftRegionTranslateTransform.X = CurrentPosX - Bounds.Width;

            _maskBorder.Opacity = CurrentPosX / Bounds.Width * 0.35;
        }
    }

    private double halfWidth
    {
        get
        {
            if (IsPanelOpened)
                return Bounds.Width * 0.7;
            return Bounds.Width * 0.3;
        }
    }

    private bool isDragging = false;
    private bool isAnimating = false;
    private bool isPressed = false;
    private Point startPoint;
    private DateTime? startTime = null;

    /// <summary>
    /// 拖动开始的水平阈值，超过才真正开始响应拖动
    /// </summary>
    public double MoveThreshold { get; set; } = 20.0;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (isAnimating) return;

        // 标记按下但尚未开始拖动
        isPressed = true;
        isDragging = IsPanelOpened;
        startPoint = e.GetCurrentPoint(this).Position;
        startTime = DateTime.Now;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        // 必须在按下后，并且不在动画中
        if (!isPressed || isAnimating)
            return;

        var current = e.GetCurrentPoint(this).Position;

        // 未达到阈值前不真正开始拖动
        if (!IsPanelOpened && !isDragging)
        {
            var deltaX = current.X - startPoint.X;
            if (Math.Abs(deltaX) < MoveThreshold)
                return;

            // 超过阈值后才开始拖动
            isDragging = true;
        }

        // 真正开始拖动后的逻辑保持不变
        double targetX = IsPanelOpened
            ? Bounds.Width - (startPoint.X - current.X)
            : current.X - startPoint.X;
        targetX = Math.Clamp(targetX, 0, Bounds.Width);
        CurrentPosX = targetX;

        e.Handled = true;
    }

    protected override async void OnPointerReleased(PointerReleasedEventArgs e)
    {
        isPressed = false;
        isAnimating = true;
        isDragging = false;

        _leftRegionTranslateTransform.Transitions = _leftTransitions;
        _mainRegionScaleTransform.Transitions = _mainTransitions;
        _maskBorder.Transitions = _maskTransitions;
        _mainRegionContentControl.Transitions = _cornerTransitions;

        if (startTime != null && DateTime.Now - startTime.Value < TimeSpan.FromMilliseconds(150))
        {
            var currentPos = e.GetCurrentPoint(this).Position;
            var deltaX = currentPos.X - startPoint.X;
            var deltaY = currentPos.Y - startPoint.Y;
            if (deltaY < 20)
            {
                if (deltaX > 30)
                {
                    IsPanelOpened = true;
                    CurrentPosX = Bounds.Width;
                }
                else if (deltaX < -30)
                {
                    IsPanelOpened = false;
                    CurrentPosX = 0;
                }
                else if (CurrentPosX > halfWidth)
                {
                    IsPanelOpened = true;
                    CurrentPosX = Bounds.Width;
                }
                else
                {
                    IsPanelOpened = false;
                    CurrentPosX = 0;
                }
            }
        }
        else
        {
            if (CurrentPosX > halfWidth)
            {
                IsPanelOpened = true;
                CurrentPosX = Bounds.Width;
            }
            else
            {
                IsPanelOpened = false;
                CurrentPosX = 0;
            }
        }

        startTime = null;

        await Task.Delay(300);

        Dispatcher.UIThread.Post(() =>
        {
            _leftRegionTranslateTransform.Transitions = null;
            _mainRegionScaleTransform.Transitions = null;
            _maskBorder.Transitions = null;
            _mainRegionContentControl.Transitions = null;
            isAnimating = false;
        });
    }
}