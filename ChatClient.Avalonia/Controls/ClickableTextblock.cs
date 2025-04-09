using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ChatClient.Avalonia.Controls;

public class ClickableTextBlock : TextBlock
{
    public static readonly StyledProperty<string> UrlProperty =
        AvaloniaProperty.Register<ClickableTextBlock, string>(nameof(Url));

    public string Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public ClickableTextBlock()
    {
        this.PointerPressed += (s, e) =>
        {
            if (!string.IsNullOrEmpty(Url))
            {
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
        };

        this.Cursor = new Cursor(StandardCursorType.Hand);
    }
}