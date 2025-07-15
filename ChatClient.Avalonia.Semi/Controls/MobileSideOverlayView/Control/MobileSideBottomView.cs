using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.EventArgs;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;

public class MobileSideBottomView : ContentControl
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
    private Border _mask = null!;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _mainRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter")!;
        _leftRegionContentControl = e.NameScope.Find<ContentPresenter>("PART_LeftContent")!;
        _mask = e.NameScope.Find<Border>("PART_Mask")!;

        _mask.PointerPressed += OnMaskPointerPressed;
    }

    private void OnMaskPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        Manager?.CloseSidePanelAsync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPanelOpenedProperty)
        {
            var leftTransform = _leftRegionContentControl.RenderTransform as TranslateTransform;

            if (IsPanelOpened)
            {
                var trans = leftTransform.Transitions;
                leftTransform.Transitions = null;
                leftTransform.Y = _leftRegionContentControl.Bounds.Height;
                leftTransform.Transitions = trans;

                _mask.IsHitTestVisible = true;
                _mask.Opacity = 0.3;
                leftTransform.Y = 0;
            }
            else
            {
                _mask.IsHitTestVisible = false;
                _mask.Opacity = 0;
                leftTransform.Y = _leftRegionContentControl.Bounds.Height;
            }

            _ = CantInteractAsync();
        }
    }

    private async Task CantInteractAsync()
    {
        _mainRegionContentControl.IsHitTestVisible = false;
        _leftRegionContentControl.IsHitTestVisible = false;
        await Task.Delay(250);
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
        if (!(dependencyObject is MobileSideBottomView view))
            throw new NullReferenceException("Dependency object is not of valid type" +
                                             nameof(MobileSideBottomView));

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

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        IsPanelOpened = true;

        // 动画跑一会儿后再恢复交互（旧的 CantInteractAsync）
        _ = CantInteractAsync();
    }

    public MobileSideBottomView()
    {
        ManagerProperty.Changed.Subscribe(x => ManagerPropertyChangedCallback(x.Sender, x));
    }
}