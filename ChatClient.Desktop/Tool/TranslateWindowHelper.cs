using System;
using System.Linq;
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
    public static void TranslateToMainWindow(IRegionManager regionManager)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // 跳转到主界面
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var windows = desktopLifetime.Windows.ToList();
                var window = App.Current.Container.Resolve<MainWindowView>();
                desktopLifetime.MainWindow = window;

                IRegionManager newRegionManager = regionManager.CreateRegionManager();
                RegionManager.SetRegionManager(window, newRegionManager);
                RegionManager.UpdateRegions();

                window.Show();

                foreach (var w in windows)
                {
                    var oldRegion = RegionManager.GetRegionManager(w);
                    if (oldRegion != null)
                        foreach (var region in oldRegion.Regions)
                        {
                            foreach (var view in region.Views)
                            {
                                if (view is IDisposable v)
                                    v.Dispose();
                            }

                            region.RemoveAll();
                        }

                    w.Close();
                    if (w is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        });
    }

    public static void TranslateToLoginWindow()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var windows = desktopLifetime.Windows.ToList();
                var window = App.Current.Container.Resolve<LoginWindowView>();
                desktopLifetime.MainWindow = window;

                window.Show();
                foreach (var wd in windows)
                {
                    var oldRegion = RegionManager.GetRegionManager(wd);
                    if (oldRegion != null)
                        foreach (var region in oldRegion.Regions)
                            region.RemoveAll();

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
        }
    }
}