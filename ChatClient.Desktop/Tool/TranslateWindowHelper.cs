using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.Login;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Tool;

public static class TranslateWindowHelper
{
    public static void TranslateToMainWindow()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // 跳转到主界面
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var windows = desktopLifetime.Windows.ToList();
                var window = App.Current.Container.Resolve<MainWindowView>();
                desktopLifetime.MainWindow = window;

                window.Show();

                foreach (var w in windows)
                {
                    w.Close();
                    if (w is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        });
    }

    public static async Task TranslateToLoginWindow()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var windows = desktopLifetime.Windows.ToList();
                var window = App.Current.Container.Resolve<LoginWindowView>();
                desktopLifetime.MainWindow = window;

                window.Show();

                foreach (var wd in windows)
                {
                    wd.Close();

                    if (wd is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        });
    }

    public static void ActivateMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Activate();
            desktop.MainWindow?.FocusManager.ClearFocus();
        }
    }

    public static void CloseAllDialog()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialogs = desktop.Windows
                .Where(d => d is SukiDialogWindow or SukiChatDialogWindow or SukiCallDialogWindow).ToList();
            dialogs.ForEach(d => d.Close());
        }
    }
}