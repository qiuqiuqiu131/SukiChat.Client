using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.EventArgs;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;

public enum SidePanelAnimationType
{
    SlideWithMain,
    FadeAndSlide
}

public class MobileSideOverlayView : ContentControl
{
    public static readonly StyledProperty<object?> SidePanelProperty =
        AvaloniaProperty.Register<MobileSideOverlayView, object?>(
            "SidePanel");

    public object? SidePanel
    {
        get => GetValue(SidePanelProperty);
        set => SetValue(SidePanelProperty, value);
    }

    public static readonly StyledProperty<bool> IsPanelOpenedProperty =
        AvaloniaProperty.Register<MobileSideOverlayView, bool>(
            "IsPanelOpened");

    public bool IsPanelOpened
    {
        get => GetValue(IsPanelOpenedProperty);
        set => SetValue(IsPanelOpenedProperty, value);
    }

    public static readonly StyledProperty<ISideViewManager> ManagerProperty =
        AvaloniaProperty.Register<MobileSideOverlayView, ISideViewManager>(
            "Manager");

    public ISideViewManager Manager
    {
        get => GetValue(ManagerProperty);
        set => SetValue(ManagerProperty, value);
    }

    public double MainPosX => -Bounds.Width * 0.5;

    private SidePanelAnimationType _animationType = SidePanelAnimationType.SlideWithMain;

    private ContentPresenter _mainRegionContentControl = null!;
    private ContentPresenter _leftRegionContentControl = null!;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _mainRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter")!;
        _leftRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_LeftContent")!;
        // _leftRegionContentControl.Transitions = null;
        // _mainRegionContentControl.Transitions = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPanelOpenedProperty)
        {
            var mainTransform = _mainRegionContentControl.RenderTransform as TranslateTransform;
            var leftTransform = _leftRegionContentControl.RenderTransform as TranslateTransform;
            if (_animationType == SidePanelAnimationType.SlideWithMain)
            {
                if (_leftRegionContentControl.Opacity != 1)
                {
                    var transitions = _leftRegionContentControl.Transitions;
                    _leftRegionContentControl.Transitions = null;
                    _leftRegionContentControl.Opacity = 1;
                    _leftRegionContentControl.Transitions = transitions;
                }

                if (IsPanelOpened)
                {
                    _leftRegionContentControl.IsVisible = true;
                    mainTransform.X = MainPosX;
                    leftTransform.X = 0;
                }
                else
                {
                    mainTransform.X = 0;
                    leftTransform.X = Bounds.Width;
                }
            }
            else if (_animationType == SidePanelAnimationType.FadeAndSlide)
            {
                if (IsPanelOpened)
                {
                    _leftRegionContentControl.IsVisible = true;
                    var transitions = _leftRegionContentControl.Transitions;
                    _leftRegionContentControl.Transitions = null;
                    _leftRegionContentControl.Opacity = 0;
                    _leftRegionContentControl.Transitions = transitions;

                    leftTransform.X = 0;
                    _leftRegionContentControl.Opacity = 1.0;
                }
                else
                {
                    var transitions = _leftRegionContentControl.Transitions;
                    _leftRegionContentControl.Transitions = null;
                    _leftRegionContentControl.Opacity = 1;
                    _leftRegionContentControl.Transitions = transitions;

                    leftTransform.X = Bounds.Width;
                    _leftRegionContentControl.Opacity = 0.5;
                }
            }

            _ = CantInteractAsync();
        }
    }

    private void AnimateDouble(double from, double to, TimeSpan duration, Action<double> setter)
    {
        var sw = Stopwatch.StartNew();
        DispatcherTimer.Run(() =>
        {
            var elapsed = sw.Elapsed;
            if (elapsed >= duration)
            {
                setter(to);
                return false;
            }

            double t = elapsed.TotalMilliseconds / duration.TotalMilliseconds;
            double eased = new QuadraticEaseInOut().Ease(t);
            setter(from + (to - from) * eased);
            return true;
        }, TimeSpan.FromMilliseconds(10), DispatcherPriority.Default);
    }

    private async Task CantInteractAsync()
    {
        _mainRegionContentControl.IsHitTestVisible = false;
        _leftRegionContentControl.IsHitTestVisible = false;
        await Task.Delay(400);
        _mainRegionContentControl.IsHitTestVisible = true;
        _leftRegionContentControl.IsHitTestVisible = true;
    }

    /// <summary>
    /// Managers the property changed callback.
    /// </summary>
    /// <param name="dependencyObject"></param>
    /// <param name="dependencyPropertyChangedEventArgs"></param>
    /// <exception cref="NullReferenceException"></exception>
    private static void ManagerPropertyChangedCallback(AvaloniaObject dependencyObject,
        AvaloniaPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        if (!(dependencyObject is MobileSideOverlayView view))
            throw new NullReferenceException("Dependency object is not of valid type " +
                                             nameof(MobileSideOverlayView));

        if (dependencyPropertyChangedEventArgs.OldValue is ISideViewManager oldManager)
            view.DetachManagerEvents(oldManager);

        if (dependencyPropertyChangedEventArgs.NewValue is ISideViewManager newManager)
            view.AttachManagerEvents(newManager);
    }

    /// <summary>
    /// Attaches the manager events.
    /// </summary>
    /// <param name="newManager">The new manager.</param>
    private void AttachManagerEvents(ISideViewManager newManager)
    {
        newManager.SideViewShowViewEvent += ManagerSideViewShowView;
        newManager.SideViewCloseViewEvent += ManagerSideViewCloseView;
    }

    /// <summary>
    /// Detaches the manager events.
    /// </summary>
    /// <param name="oldManager">The old manager.</param>
    private void DetachManagerEvents(ISideViewManager oldManager)
    {
        oldManager.SideViewShowViewEvent -= ManagerSideViewShowView;
        oldManager.SideViewCloseViewEvent -= ManagerSideViewCloseView;
    }

    private async Task ManagerSideViewCloseView(object? sender, SideViewCloseViewEventArgs e)
    {
        if (IsPanelOpened)
            IsPanelOpened = false;
        await Task.Delay(250).ConfigureAwait(true);
        if (SidePanel != null)
            SidePanel = null;
        _leftRegionContentControl.IsVisible = false;
    }

    private async Task ManagerSideViewShowView(object? sender, SideViewShowViewEventArgs e)
    {
        if (e.NewView is null)
            throw new ArgumentNullException(nameof(e.NewView), "New view cannot be null");
        if (e.NewView is not UserControl view)
            throw new ArgumentException("New view must be a UserControl", nameof(e.NewView));

        _animationType = e.AnimationType;

        // 1. 先插入视图
        SidePanel = view;
        UpdateLayout();

        // 2. 等待一次 Render pass 完成，确保 UI 已经测量、排列、渲染
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        IsPanelOpened = true;

        // 动画跑一会儿后再恢复交互（旧的 CantInteractAsync）
        _ = CantInteractAsync();
    }

    public MobileSideOverlayView()
    {
        ManagerProperty.Changed.Subscribe(x => ManagerPropertyChangedCallback(x.Sender, x));
    }
}