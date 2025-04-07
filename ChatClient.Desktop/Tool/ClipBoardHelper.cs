using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace ChatClient.Desktop.Tool;

public static class ClipBoardHelper
{
    public static void AddText(string text)
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Clipboard.SetTextAsync(text);
        }
    }
}