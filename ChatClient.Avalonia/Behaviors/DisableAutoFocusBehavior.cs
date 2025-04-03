using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class DisableAutoFocusBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        AssociatedObject.GotFocus += OnGotFocus;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.GotFocus -= OnGotFocus;
    }

    private void OnGotFocus(object sender, GotFocusEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Unspecified)
        {
            AssociatedObject.Focusable = false;
            AssociatedObject.Focusable = true;
            e.Handled = true;
        }
    }
}