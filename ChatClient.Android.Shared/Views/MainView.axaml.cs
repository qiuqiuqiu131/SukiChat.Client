using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChatClient.Android.Shared.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // 确保 View 填充到系统栏区域
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
                // insetsManager.DisplayEdgeToEdge = true;
                // insetsManager.SystemBarColor = Colors.Transparent;
            }
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
    }
}