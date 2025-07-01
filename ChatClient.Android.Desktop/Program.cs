using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Dialogs;
using ChatClient.Android.Shared;

namespace ChatClient.Android.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var app = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // 此处是添加了一个内置默认字体
            .WithInterFont()
            .LogToTrace();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();
        return app;
    }
}