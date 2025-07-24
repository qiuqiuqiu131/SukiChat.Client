using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ChatClient.Avalonia.Utility;

namespace ChatClient.Avalonia.Controls.MobileScrollViewer;

public enum ScrollMode
{
    Vertical,
    Horizontal,
    Both
}

public enum ElasticDirection
{
    Top,
    Bottom,
    Left,
    Right,
    None // 不弹性
}

public class MobileScrollViewer : ScrollViewer
{
    public static readonly StyledProperty<double> ElasticityProperty =
        AvaloniaProperty.Register<MobileScrollViewer, double>(
            "Elasticity", defaultValue: 0.5);

    public double Elasticity
    {
        get => GetValue(ElasticityProperty);
        set => SetValue(ElasticityProperty, value);
    }

    public static readonly StyledProperty<double> MoveThresholdProperty =
        AvaloniaProperty.Register<MobileScrollViewer, double>(
            "MoveThreshold", defaultValue: 20);

    public double MoveThreshold
    {
        get => GetValue(MoveThresholdProperty);
        set => SetValue(MoveThresholdProperty, value);
    }

    public static readonly StyledProperty<ScrollMode> ScrollModeProperty =
        AvaloniaProperty.Register<MobileScrollViewer, ScrollMode>(
            "ScrollMode", defaultValue: ScrollMode.Vertical);

    public ScrollMode ScrollMode
    {
        get => GetValue(ScrollModeProperty);
        set => SetValue(ScrollModeProperty, value);
    }

    /// <summary>
    /// 可移动的最大偏移量Y
    /// </summary>
    public double MaxOffsetY
    {
        get
        {
            var offsetY = (((Control?)Content)?.Bounds.Height ?? 0) - Bounds.Height;
            if (offsetY < 0) offsetY = 0;
            return offsetY;
        }
    }

    const double inertiaResistance = 0.2; // 指数衰减系数
    const double inertiaSpeedEnd = 10.0; // 最小结束速度(px/s)
    const double elasticityDefault = 0.0015; // 弹性位移的默认系数

    private Point _startPoint; // 拖动开始时的起点
    private bool _isPressed; // 是否正在按下状态
    private bool _isDragging;

    private Point _lastPoint; // 上一次拖动的位置
    private VelocityTracker? _velocityTracker;
    private Vector _inertia;
    private ulong? _lastMoveTimestamp; // 新增：记录最后一次移动时间戳
    private int _pointerId;

    private bool _isElastic; // 是否处于弹性位移状态
    private Point _startElasticPoint; // 弹性位移开始时的起点
    private ElasticDirection _elasticDirection = ElasticDirection.None;

    // 用来渲染弹性位移
    private readonly TranslateTransform _elasticTransform = new();

    private readonly Transitions _elasticTransitions =
    [
        new DoubleTransition
        {
            Property = TranslateTransform.XProperty,
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = new CubicEaseInOut()
        },

        new DoubleTransition
        {
            Property = TranslateTransform.YProperty,
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = new CubicEaseInOut()
        }
    ];

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Control>("PART_ContentPresenter") is Control cp)
            cp.RenderTransform = _elasticTransform;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _inertia = Vector.Zero; // 重置惯性速度

        _isPressed = true;
        _isElastic = false;
        _isDragging = false;
        _startPoint = e.GetPosition(TopLevel.GetTopLevel(this));
        _lastPoint = _startPoint;
        _pointerId = e.Pointer.Id;
        _velocityTracker = new VelocityTracker();
        _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds((double)e.Timestamp), new Vector());
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        // 仅处理自己捕获的指针
        if (e.Pointer.Id != _pointerId || !_isPressed)
            return;

        // 当前指针位置
        var current = e.GetPosition(TopLevel.GetTopLevel(this));

        // 1. 判断是否达到拖动条件，达到阈值才算真正开始拖动
        var deltaFromStart = current - _startPoint; // 累计位移
        if (!_isDragging)
        {
            // Vertical 模式下，横向滑动超过阈值但竖直方向未超过，则取消捕获
            if (ScrollMode == ScrollMode.Vertical
                && Math.Abs(deltaFromStart.X) > MoveThreshold
                && Math.Abs(deltaFromStart.Y) <= MoveThreshold)
            {
                _isPressed = false;
                return;
            }

            // Horizontal 模式下，竖直滑动超过阈值但横向未超过，则取消捕获
            if (ScrollMode == ScrollMode.Horizontal
                && Math.Abs(deltaFromStart.Y) > MoveThreshold
                && Math.Abs(deltaFromStart.X) <= MoveThreshold)
            {
                _isPressed = false;
                return;
            }

            // 达到阈值才算真正开始拖动
            double check = ScrollMode switch
            {
                ScrollMode.Vertical => Math.Abs(deltaFromStart.Y),
                ScrollMode.Horizontal => Math.Abs(deltaFromStart.X),
                ScrollMode.Both => Math.Sqrt(Math.Pow(deltaFromStart.X, 2) + Math.Pow(deltaFromStart.Y, 2)),
                _ => 0
            };
            if (check <= MoveThreshold)
                return;

            _isDragging = true;
        }

        // 2. 正常滚动部分：将ScrollViewer的Offset设置为新的位置
        if (!_isElastic)
        {
            // 2.1 计算新的偏移量
            var delta = current - _lastPoint;
            var proposed = Offset;
            if (ScrollMode is ScrollMode.Vertical or ScrollMode.Both)
                proposed = new Vector(proposed.X, proposed.Y - delta.Y);
            if (ScrollMode is ScrollMode.Horizontal or ScrollMode.Both)
                proposed = new Vector(proposed.X - delta.X, proposed.Y);
            Offset = proposed;

            // 2.2 计算是否到边界
            bool needElastic = NeedElastic();
            if (needElastic)
            {
                _isElastic = true;
                _startElasticPoint = current;
            }
        }
        else // 3. 弹性位移部分,到达边界弹性拖拽
        {
            // 计算弹性位移
            var delta = current - _startElasticPoint;
            if (ScrollMode is ScrollMode.Horizontal or ScrollMode.Both)
            {
                if (_elasticDirection == ElasticDirection.Left && delta.X > 0
                    || _elasticDirection == ElasticDirection.Right && delta.X < 0)
                {
                    // 计算弹性位移
                    var dynE = Elasticity / (1 + Math.Abs(delta.X) * elasticityDefault);
                    _elasticTransform.X = delta.X * dynE;
                }
                else
                {
                    _isElastic = false;
                    _elasticTransform.X = 0; // 清除弹性位移
                }

                _elasticTransform.Y = 0; // 水平滚动时清除垂直弹性位移
            }
            else if (ScrollMode is ScrollMode.Both or ScrollMode.Vertical)
            {
                if (_elasticDirection == ElasticDirection.Top && delta.Y > 0
                    || _elasticDirection == ElasticDirection.Bottom && delta.Y < 0)
                {
                    // 计算弹性位移
                    var dynE = Elasticity / (1 + Math.Abs(delta.Y) * elasticityDefault);
                    _elasticTransform.Y = delta.Y * dynE;
                }
                else
                {
                    _isElastic = false;
                    _elasticTransform.Y = 0; // 清除弹性位移
                }

                _elasticTransform.X = 0; // 垂直滚动时清除水平弹性位移
            }
        }

        // 4. 记录速度，用于惯性
        _velocityTracker?.AddPosition(
            TimeSpan.FromMilliseconds((double)e.Timestamp),
            deltaFromStart);

        // 记录最后一次移动的时间戳
        _lastMoveTimestamp = e.Timestamp;

        _lastPoint = current;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isDragging = false;
        _isPressed = false;

        if (e.Pointer.Id != _pointerId)
            return;

        if (_isElastic)
        {
            _isElastic = false;

            // 弹回 transform
            _elasticTransform.Transitions = _elasticTransitions;
            _elasticTransform.X = 0;
            _elasticTransform.Y = 0;

            _ = Task.Delay(200).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() => { _elasticTransform.Transitions = null; });
            });
        }
        else
        {
            // 获取初始惯性速度(px/s)
            var inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;

            // 加上与 ScrollGestureRecognizer 一致的判断
            if (inertia != Vector.Zero
                && e.Timestamp != 0UL
                && _lastMoveTimestamp.HasValue
                && (e.Timestamp - _lastMoveTimestamp.Value) <= 200UL)
            {
                StartInertia();
            }
        }
    }

    // 判断是否需要弹性位移
    private bool NeedElastic()
    {
        var maxX = Math.Max(0, Extent.Width - Viewport.Width);
        var maxY = Math.Max(0, Extent.Height - Viewport.Height);
        bool atLeft = Offset.X <= 0;
        bool atRight = Offset.X >= maxX;
        bool atTop = Offset.Y <= 0;
        bool atBottom = Offset.Y >= maxY;
        if (ScrollMode is ScrollMode.Both or ScrollMode.Vertical)
        {
            if (atTop)
            {
                _elasticDirection = ElasticDirection.Top;
                return true;
            }

            if (atBottom)
            {
                _elasticDirection = ElasticDirection.Bottom;
                return true;
            }
        }

        if (ScrollMode is ScrollMode.Both or ScrollMode.Horizontal)
        {
            if (atLeft)
            {
                _elasticDirection = ElasticDirection.Left;
                return true;
            }

            if (atRight)
            {
                _elasticDirection = ElasticDirection.Right;
                return true;
            }
        }

        _elasticDirection = ElasticDirection.None;
        return false;
    }

    // 惯性移动
    private void StartInertia()
    {
        if (_velocityTracker == null)
            return;

        _inertia = _velocityTracker.GetFlingVelocity().PixelsPerSecond;

        if (_inertia == new Vector())
            return;

        var st = Stopwatch.StartNew();
        TimeSpan last = TimeSpan.Zero;

        // 用 DispatcherTimer.Run 实现指数衰减惯性
        DispatcherTimer.Run(() =>
            {
                var span = st.Elapsed - last;
                last = st.Elapsed;

                // 指数衰减速度
                var v = _inertia * Math.Pow(inertiaResistance, st.Elapsed.TotalSeconds);

                // 计算位移
                var delta = v * span.TotalSeconds;
                var off = Offset;
                if (ScrollMode is ScrollMode.Vertical or ScrollMode.Both)
                    off = new Vector(off.X, off.Y - delta.Y);
                if (ScrollMode is ScrollMode.Horizontal or ScrollMode.Both)
                    off = new Vector(off.X - delta.X, off.Y);
                Offset = off;

                // 速度低于阈值时结束惯性
                var stopY = ScrollMode is ScrollMode.Vertical or ScrollMode.Both && Math.Abs(v.Y) <= inertiaSpeedEnd;
                var stopX = ScrollMode is ScrollMode.Horizontal or ScrollMode.Both && Math.Abs(v.X) <= inertiaSpeedEnd;
                if (stopY || stopX || NeedElastic())
                    return false;

                return true;
            },
            TimeSpan.FromMilliseconds(3),
            DispatcherPriority.Default);
    }
}