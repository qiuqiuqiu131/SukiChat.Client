using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactivity;
using ChatClient.Tool.Tools;

namespace ChatClient.Avalonia.Behaviors;

public class ImageShowByClickBehavior : Behavior<Control>
{
    public static StyledProperty<Image> TargetImageProperty
        = AvaloniaProperty.Register<ImageShowByClickBehavior, Image>(nameof(TargetImage));

    public Image TargetImage
    {
        get => GetValue(TargetImageProperty);
        set => SetValue(TargetImageProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed += OnPointerPressed;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TargetImage == null) return;
        Bitmap bitmap = (Bitmap)TargetImage.Source;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }
}