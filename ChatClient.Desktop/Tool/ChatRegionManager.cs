using System;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Common;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Tool;

public static class ChatRegionManager
{
    public static void RequestNavigate(string regionName, string viewName)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var window = desktopLifetime.MainWindow;
            var _regionManager = RegionManager.GetRegionManager(window);
            _regionManager.RequestNavigate(regionName, viewName);
        }
    }
    
    public static void RequestNavigate(string regionName, string viewName,INavigationParameters parameters)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var window = desktopLifetime.MainWindow;
            var _regionManager = RegionManager.GetRegionManager(window);
            _regionManager.RequestNavigate(regionName, viewName,parameters);
        }
    }
    
    public static void RequestNavigate(string regionName, string viewName, Action<NavigationResult> navigationCallback)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var window = desktopLifetime.MainWindow;
            var _regionManager = RegionManager.GetRegionManager(window);
            _regionManager.RequestNavigate(regionName, viewName, navigationCallback);
        }
    }
    
    public static void RequestNavigate(string regionName, string viewName, Action<NavigationResult> navigationCallback,INavigationParameters parameters)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var window = desktopLifetime.MainWindow;
            var _regionManager = RegionManager.GetRegionManager(window);
            _regionManager.RequestNavigate(regionName, viewName, navigationCallback,parameters);
        }
    }
}