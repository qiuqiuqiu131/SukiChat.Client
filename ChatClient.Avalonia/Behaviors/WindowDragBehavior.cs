using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class WindowDragBehavior : Behavior<Control>
{
    private Window? _window;
    private Point _startPoint;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PointerPressed += OnPointerPressed;
        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerReleased += OnPointerReleased;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PointerPressed -= OnPointerPressed;
        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerReleased -= OnPointerReleased;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
        {
            _window = AssociatedObject.GetVisualRoot() as Window;
            if (_window != null)
                _startPoint = e.GetPosition(_window);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_window != null && e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(_window);
            var offset = currentPoint - _startPoint;
            _window.Position = new PixelPoint((int)(_window.Position.X + offset.X), (int)(_window.Position.Y + offset.Y));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_window != null)
            _window = null;
    }
}