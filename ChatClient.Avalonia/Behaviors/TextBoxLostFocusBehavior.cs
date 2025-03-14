using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class TextBoxLostFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            // 订阅 KeyDown 事件
            AssociatedObject.KeyDown += OnTextBoxKeyDown;
            AssociatedObject.GotFocus += AssociatedObjectOnGotFocus;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            // 取消订阅 KeyDown 事件
            AssociatedObject.KeyDown -= OnTextBoxKeyDown;
            AssociatedObject.GotFocus -= AssociatedObjectOnGotFocus;
        }
    }

    private void AssociatedObjectOnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        // 检查是否按下 Enter 键
        if (e.Key == Key.Enter)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.FocusManager.ClearFocus();
            }
        }
    }
}