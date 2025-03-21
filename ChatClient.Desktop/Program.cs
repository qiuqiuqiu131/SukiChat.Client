using Avalonia;
using System;
using Avalonia.Dialogs;
using Avalonia.Media;

namespace ChatClient.Desktop;

sealed class Program
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
            // // 此处配置自定义字体
            // .With(new FontManagerOptions
            // {
            //     // 设置为默认字体
            //     DefaultFamilyName = "avares://ChatClient.Desktop/Assets/MiSans-Normal.ttf#MiSans"
            // })
            .LogToTrace();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();
        return app;
    }
}