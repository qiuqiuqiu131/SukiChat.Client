using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using ChatClient.Android.Shared;
using ChatClient.Android.Shared.Event;
using ChatClient.Android.Shared.Tool;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.Android;

[Activity(
    Label = "SukiChat",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/Icon",
    HardwareAccelerated = true,
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private IEventAggregator _eventAggregator;

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightFollowSystem; // 默认深色主题
        base.OnCreate(savedInstanceState);
        EdgeToEdge.Enable(this);

        // 注册系统返回事件
        var app = App.Current as PrismApplication;
        _eventAggregator = app.Container.Resolve<IEventAggregator>();
        BackRequested += OnBackRequestEvent;
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        // 配置变化时调用（如主题切换）
        UpdateTheme(newConfig);
    }

    private void OnBackRequestEvent(object? sender, AndroidBackRequestedEventArgs args)
    {
        // 触发系统返回事件
        _eventAggregator.GetEvent<AndroidBackRequestEvent>().Publish(args);
    }

    private void UpdateTheme(Configuration newConfig)
    {
        var currentNightMode = newConfig.UiMode & UiMode.NightMask;

        var app = App.Current;
        switch (currentNightMode)
        {
            case UiMode.NightYes:
                // 切换到深色主题
                app.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case UiMode.NightNo:
                // 切换到浅色主题
                app.RequestedThemeVariant = ThemeVariant.Light;
                break;
            default:
                // 跟随系统
                app.RequestedThemeVariant = ThemeVariant.Default;
                break;
        }
    }
}