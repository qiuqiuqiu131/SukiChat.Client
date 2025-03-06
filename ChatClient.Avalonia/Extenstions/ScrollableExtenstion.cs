using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using SukiUI.Theme;

namespace ChatClient.Avalonia.Extenstions;

public static class ItemsControlExtensions
{
    public static readonly AttachedProperty<bool> AnimatedScrollProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("AnimatedScroll", typeof(ItemsControl),
            defaultValue: false);

    static ItemsControlExtensions()
    {
        AnimatedScrollProperty.Changed.AddClassHandler<ItemsControl>(HandleAnimatedScrollChanged);
    }

    private static void HandleAnimatedScrollChanged(ItemsControl interactElem, AvaloniaPropertyChangedEventArgs args)
    {
        if (GetAnimatedScroll(interactElem))
            interactElem.AttachedToVisualTree += (sender, args) =>
                Scrollable.MakeScrollable(ElementComposition.GetElementVisual(interactElem));
        else
            interactElem.AttachedToVisualTree += (sender, args) =>
                ElementComposition.GetElementVisual(interactElem).ImplicitAnimations.Remove("Offset");
    }

    public static bool GetAnimatedScroll(ItemsControl wrap)
    {
        return wrap.GetValue(AnimatedScrollProperty);
    }

    public static void SetAnimatedScroll(ItemsControl wrap, bool value)
    {
        wrap.SetValue(AnimatedScrollProperty, value);
    }
}