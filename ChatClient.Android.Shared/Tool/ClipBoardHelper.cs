using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ChatClient.Android.Shared.Tool;

public static class ClipBoardHelper
{
    public static void AddText(string text)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Clipboard?.SetTextAsync(text);
        }
        else if (App.Current.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var topLevel = TopLevel.GetTopLevel(singleView.MainView);
            if (topLevel != null)
            {
                topLevel.Clipboard?.SetTextAsync(text);
            }
        }
    }
}