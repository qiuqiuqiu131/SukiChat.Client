using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace ChatClient.Avalonia.Controls.OverlaySplitView;

public class OverlaySplitView : ContentControl
{
    public static readonly StyledProperty<object> PaneContentProperty =
        AvaloniaProperty.Register<OverlaySplitView, object>(nameof(PaneContent));

    public static readonly StyledProperty<bool> IsPaneOpenProperty =
        AvaloniaProperty.Register<OverlaySplitView, bool>(nameof(IsPaneOpen));

    public static readonly StyledProperty<double> PaneWidthProperty =
        AvaloniaProperty.Register<OverlaySplitView, double>(nameof(PaneWidth), 300);

    public object PaneContent
    {
        get => GetValue(PaneContentProperty);
        set => SetValue(PaneContentProperty, value);
    }

    public bool IsPaneOpen
    {
        get => GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public double PaneWidth
    {
        get => GetValue(PaneWidthProperty);
        set => SetValue(PaneWidthProperty, value);
    }

    private Border PaneContainer;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PaneContainer = e.NameScope.Get<Border>("PaneContainer");
        PaneContainer.PointerPressed += PaneContainerOnPointerPressed;
    }

    private void PaneContainerOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.FocusManager.ClearFocus();
        }
    }
}