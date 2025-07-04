using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class OpenFileInExplorerBehavior : Behavior<Control>
{
    public static readonly StyledProperty<string> FilePathProperty =
        AvaloniaProperty.Register<OpenFileInExplorerBehavior, string>(nameof(FilePath));

    public string FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PointerPressed += OnClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PointerPressed -= OnClick;
    }

    private void OnClick(object? sender, PointerPressedEventArgs e)
    {
        if (!string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath))
        {
            var argument = $"/select,\"{FilePath}\"";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
            {
                UseShellExecute = true
            });
        }
    }
}