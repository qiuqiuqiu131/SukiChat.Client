using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class SafeAreaPaddingBehavior : Behavior<TemplatedControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AttachedToVisualTree += OnLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.AttachedToVisualTree -= OnLoaded;
        base.OnDetaching();
    }

    private void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TemplatedControl control)
        {
            var topLevel = TopLevel.GetTopLevel(control);
            var insets = topLevel?.InsetsManager?.SafeAreaPadding.Top ?? 0;
            insets = 37;
            var p = control.Padding;
            control.Padding = new Thickness(p.Left, insets, p.Right, p.Bottom);
        }
    }
}