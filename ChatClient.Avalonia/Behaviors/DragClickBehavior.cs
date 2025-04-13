using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class DragClickBehavior : Behavior<Control>
{
    // 长按阈值 (毫秒)
    private const int LongPressThreshold = 100;

    // 用于标识是否正在进行拖拽
    private bool _isDragging;

    // 用于计时的秒表
    private Stopwatch _stopwatch = new Stopwatch();

    // 鼠标按下的初始位置
    private Point _startPoint;

    // 关联的控件在拖拽开始时的位置
    private Point _originalPosition;

    // 取消拖拽的Token
    private CancellationTokenSource _dragCancellation;

    // 是否启用拖拽功能
    public static readonly StyledProperty<bool> EnableDragProperty =
        AvaloniaProperty.Register<DragClickBehavior, bool>(nameof(EnableDrag), true);

    public bool EnableDrag
    {
        get => GetValue(EnableDragProperty);
        set => SetValue(EnableDragProperty, value);
    }

    // 拖拽容器 - 限制拖拽范围
    public static readonly StyledProperty<Control> DragContainerProperty =
        AvaloniaProperty.Register<DragClickBehavior, Control>(nameof(DragContainer));

    public Control DragContainer
    {
        get => GetValue(DragContainerProperty);
        set => SetValue(DragContainerProperty, value);
    }

    // 点击命令
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<DragClickBehavior, ICommand>(nameof(Command));

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    // 命令参数
    public static readonly StyledProperty<object> CommandParameterProperty =
        AvaloniaProperty.Register<DragClickBehavior, object>(nameof(CommandParameter));

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed += OnPointerPressed;
            AssociatedObject.PointerReleased += OnPointerReleased;
            AssociatedObject.PointerMoved += OnPointerMoved;
            AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.PointerMoved -= OnPointerMoved;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        }

        _dragCancellation?.Cancel();
        _dragCancellation?.Dispose();
        _dragCancellation = null;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            // 获取当前鼠标位置和控件位置
            _startPoint = e.GetPosition(null);

            // 明确获取 Canvas 附加属性
            var left = AssociatedObject.GetValue(Canvas.LeftProperty);
            var top = AssociatedObject.GetValue(Canvas.TopProperty);

            // 如果未设置初始位置，则设置默认值
            _originalPosition = new Point(
                left != null ? (double)left : 0,
                top != null ? (double)top : 0
            );

            // 确保在拖拽前设置这些属性（如果之前未设置）
            if (left == null)
                AssociatedObject.SetValue(Canvas.LeftProperty, _originalPosition.X);
            if (top == null)
                AssociatedObject.SetValue(Canvas.TopProperty, _originalPosition.Y);

            // 开始计时
            _stopwatch.Restart();

            // 捕获鼠标
            e.Pointer.Capture(AssociatedObject);

            // 日志输出（可选，用于调试）
            // Console.WriteLine($"按下：位置 X={_originalPosition.X}, Y={_originalPosition.Y}");

            // 启动延迟检测是否为长按
            _dragCancellation = new CancellationTokenSource();
            DetectLongPress(_dragCancellation.Token);
        }
    }

    private async void DetectLongPress(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(LongPressThreshold, cancellationToken);

            // 如果延迟后仍然没有取消，则认为是长按，开始拖拽
            if (!cancellationToken.IsCancellationRequested && EnableDrag)
            {
                _isDragging = true;
                Console.WriteLine("开始拖拽");
            }
        }
        catch (TaskCanceledException)
        {
            // 任务被取消，忽略
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDragging && e.Pointer.Captured == AssociatedObject)
        {
            var currentPoint = e.GetPosition(null);
            var deltaX = currentPoint.X - _startPoint.X;
            var deltaY = currentPoint.Y - _startPoint.Y;

            // 计算新位置
            double newLeft = _originalPosition.X + deltaX;
            double newTop = _originalPosition.Y + deltaY;

            // 如果有容器限制，则检查边界
            if (DragContainer != null)
            {
                // 获取容器的边界
                var containerBounds = DragContainer.Bounds;

                // 获取当前控件的宽高
                double elementWidth = AssociatedObject.Bounds.Width;
                double elementHeight = AssociatedObject.Bounds.Height;

                // 计算容器相对于窗口的位置
                var containerPosition = DragContainer.TranslatePoint(new Point(0, 0), null) ?? new Point(0, 0);

                // 约束左边界
                if (newLeft < containerPosition.X)
                    newLeft = containerPosition.X;

                // 约束右边界 (容器左边界 + 容器宽度 - 元素宽度)
                if (newLeft + elementWidth > containerPosition.X + containerBounds.Width)
                    newLeft = containerPosition.X + containerBounds.Width - elementWidth;

                // 约束上边界
                if (newTop < containerPosition.Y)
                    newTop = containerPosition.Y;

                // 约束下边界 (容器上边界 + 容器高度 - 元素高度)
                if (newTop + elementHeight > containerPosition.Y + containerBounds.Height)
                    newTop = containerPosition.Y + containerBounds.Height - elementHeight;
            }

            // 更新控件位置
            AssociatedObject.SetValue(Canvas.LeftProperty, newLeft);
            AssociatedObject.SetValue(Canvas.TopProperty, newTop);

            // 日志输出（可选，用于调试）
            // Console.WriteLine($"拖拽中：位置 X={newLeft}, Y={newTop}");

            e.Handled = true;
        }
    }

    private async void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _stopwatch.Stop();
            bool wasShortClick = _stopwatch.ElapsedMilliseconds < LongPressThreshold;

            // 取消捕获
            e.Pointer.Capture(null);

            // 如果是短按并且命令可用，则执行命令
            if (wasShortClick && Command != null && !_isDragging)
            {
                Command.Execute(CommandParameter);
            }

            // 重置拖拽状态
            _isDragging = false;

            // 取消长按检测
            _dragCancellation?.Cancel();
            _dragCancellation?.Dispose();
            _dragCancellation = null;

            // 日志输出（可选，用于调试）
            // Console.WriteLine($"释放：位置 X={_originalPosition.X}, Y={_originalPosition.Y}");

            e.Handled = true;
        }
    }

    private void OnPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
    {
        // 重置拖拽状态
        _isDragging = false;

        // 取消长按检测
        _dragCancellation?.Cancel();
        _dragCancellation?.Dispose();
        _dragCancellation = null;
    }
}