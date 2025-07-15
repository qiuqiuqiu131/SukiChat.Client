using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace ChatClient.Avalonia.Controls.QScrollViewer;

/// <summary>
/// 垂直方向上的平缓滚动视图控件
/// </summary>
public class QScrollViewer : ScrollViewer
{
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

    private double currentPos; // 当前滚动的位置

    public double CurrentPos
    {
        get => currentPos;
        set
        {
            if (value < 0) currentPos = 0;
            else
            {
                var maxValue = MaxOffsetY;
                if (value > maxValue)
                    currentPos = maxValue;
                else
                    currentPos = value;
            }
        }
    }

    // 正在移动到最顶部或最底部
    public bool IsToMoving { get; private set; } = false;

    private bool isLocked = false; // 是否锁定滚动
    private double lockDistance = 0; // 锁定时与底部的距离

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (Content is Control control)
        {
            // 监听Content的Bounds属性变化
            control.PropertyChanged += OnContentPropertyChanged;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (Content is Control control)
        {
            control.PropertyChanged -= OnContentPropertyChanged;
        }
    }

    /// <summary>
    /// ScrollViewer的Content属性发生变化时触发
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Control.BoundsProperty)
        {
            // 锁上了，那么要保证与底部距离保持不变
            if (isLocked)
            {
                double offsetY = MaxOffsetY - lockDistance;
                Offset = new Vector(Offset.X, offsetY);
                CurrentPos = offsetY;
            }
        }
    }

    #region Lock

    public void Lock()
    {
        if (AnimationToken != null)
            AnimationToken.Cancel();
        isLocked = true;
        lockDistance = MaxOffsetY - CurrentPos;
    }

    public void UnLock()
    {
        isLocked = false;
    }

    #endregion

    #region Scroll

    /// <summary>
    /// 向上移动指定的偏移量
    /// </summary>
    /// <param name="offset"></param>
    public void MoveUp(double offset)
    {
        if (IsToMoving || isLocked) return;
        _ = ScrollMove(CurrentPos + offset);
    }

    /// <summary>
    /// 向下移动指定的偏移量
    /// </summary>
    /// <param name="offset"></param>
    public void MoveDown(double offset)
    {
        if (IsToMoving || isLocked) return;
        _ = ScrollMove(CurrentPos - offset);
    }

    /// <summary>
    /// 移动到底部
    /// </summary>
    public async void MoveToBottom()
    {
        if (isLocked) return;

        IsToMoving = true;
        await ScrollMove(MaxOffsetY);
        IsToMoving = false;
    }

    /// <summary>
    /// 移动到顶部
    /// </summary>
    public async void MoveToTop()
    {
        if (isLocked) return;

        IsToMoving = true;
        await ScrollMove(0);
        IsToMoving = false;
    }

    public void ScrollToBottom()
    {
        CurrentPos = MaxOffsetY;
        base.ScrollToEnd();
    }

    #endregion

    #region Anim

    private CancellationTokenSource? AnimationToken;

    private async Task ScrollMove(double target, int maxDuration = 400)
    {
        CurrentPos = target;

        // 创建动画
        double time = Math.Abs(target - Offset.Y) * 2;
        if (time > maxDuration) time = maxDuration;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(time),
            FillMode = FillMode.Forward,
            Easing = new SineEaseOut(),
            IterationCount = new IterationCount(1),
            PlaybackDirection = PlaybackDirection.Normal,
            Children =
            {
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter { Property = OffsetProperty, Value = Offset }
                    },
                    Cue = new Cue(0)
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = OffsetProperty,
                            Value = new Vector(Offset.X, target)
                        }
                    },
                    Cue = new Cue(1)
                }
            }
        };

        // 启动动画
        if (AnimationToken != null)
            AnimationToken.Cancel();
        AnimationToken = new CancellationTokenSource();
        await anim.RunAsync(this, AnimationToken.Token);
    }

    #endregion
}