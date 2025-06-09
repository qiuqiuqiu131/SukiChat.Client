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
        AssociatedObject.DoubleTapped += OnDoubleTrapped;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PointerPressed -= OnPointerPressed;
        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerReleased -= OnPointerReleased;
        AssociatedObject.DoubleTapped -= OnDoubleTrapped;
        _window = null;
    }

    private void OnDoubleTrapped(object? sender, TappedEventArgs e)
    {
        // _window = AssociatedObject.GetVisualRoot() as Window;
        // if (_window != null)
        // {
        //     if (_window.WindowState == WindowState.Maximized)
        //         _window.WindowState = WindowState.Normal;
        // }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _window = AssociatedObject.GetVisualRoot() as Window;
        if (_window.WindowState != WindowState.Normal)
            return;

        if (e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
        {
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
            _window.Position =
                new PixelPoint((int)(_window.Position.X + offset.X), (int)(_window.Position.Y + offset.Y));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _window = null;
    }
}