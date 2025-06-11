using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ChatClient.Avalonia.Extenstions;

public static class SelectableTextBlockExtensions
{
    public static readonly AttachedProperty<bool> AllowRightClickProperty =
        AvaloniaProperty.RegisterAttached<SelectableTextBlock, bool>(
            "AllowRightClick", typeof(SelectableTextBlockExtensions));

    static SelectableTextBlockExtensions()
    {
        AllowRightClickProperty.Changed.AddClassHandler<SelectableTextBlock>(
            (stb, e) => OnAllowRightClickChanged(stb, e));
    }

    private static void OnAllowRightClickChanged(SelectableTextBlock stb, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            stb.PointerReleased += OnPointerReleased;
        }
        else
        {
            stb.PointerReleased -= OnPointerReleased;
        }
    }

    private static void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            e.Handled = false;
        }
    }

    public static void SetAllowRightClick(SelectableTextBlock element, bool value)
    {
        element.SetValue(AllowRightClickProperty, value);
    }

    public static bool GetAllowRightClick(SelectableTextBlock element)
    {
        return element.GetValue(AllowRightClickProperty);
    }
}