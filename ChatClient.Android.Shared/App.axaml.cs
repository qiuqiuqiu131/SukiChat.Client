using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using ChatClient.Android.Shared.Services;
using ChatClient.Android.Shared.Tool;
using ChatClient.Android.Shared.ViewModels;
using ChatClient.Android.Shared.Views;
using ChatClient.Android.Shared.Views.ForgetPasswordView;
using ChatClient.Android.Shared.Views.RegisterView;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;
using ChatClient.BaseService.SqlSugar;
using ChatClient.DataBase.SqlSugar.SugarDB;
using ChatClient.Resources;
using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Microsoft.Extensions.Configuration;
using Prism.DryIoc;
using SocketClient;
#if ANDROID
using AndApplication = Android.App.Application;
#endif

namespace ChatClient.Android.Shared;

public class App : PrismApplication
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    /// <summary>
    ///  添加模块
    /// </summary>
    /// <param name="moduleCatalog"></param>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<ClientModule>();
        moduleCatalog.AddModule<ResourcesModule>();
        moduleCatalog.AddModule<SqlSugarServiceModule>();
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="containerRegistry"></param>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 确保配置文件存在与Files目录下
#if ANDROID
        FileTool.CopyAssetsToFilesIfNeeded(
        [
            "appsettings.json",
            "appsettings.webrtc.json",
        ]);

        // 注册配置文件
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AndApplication.Context.FilesDir.AbsolutePath, "appsettings.json"), optional: false,
                reloadOnChange: true)
            .AddJsonFile(Path.Combine(AndApplication.Context.FilesDir.AbsolutePath, "appsettings.webrtc.json"),
                optional: false, reloadOnChange: true)
            .Build();
#else
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.webrtc.json", optional: false, reloadOnChange: true)
            .Build();
#endif

        var appsettings = new AppSettings();
        configuration.Bind(appsettings);
        containerRegistry.RegisterInstance(appsettings);

        // 注册持久化文件保存路径
#if ANDROID
        containerRegistry.RegisterSingleton<IAppDataManager, AndroidAppDataManager>();
#else
        containerRegistry.RegisterSingleton<IAppDataManager, DesktopAppDataManager>();
#endif

        // 注册数据库
        containerRegistry.RegisterSugarDataBase();

        // 注册NotificationManager
        containerRegistry.RegisterSingleton<INotificationMessageManager, NotificationMessageManager>();
        containerRegistry.RegisterSingleton<ISideOverlayViewManager, SideViewManager>();
        containerRegistry.RegisterSingleton<ISideBottomViewManager, SideViewManager>();

        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
        containerRegistry.RegisterForNavigation<NetSettingView, NetSettingViewModel>();
        containerRegistry.RegisterForNavigation<RegisterView, RegisterViewModel>();
        containerRegistry.RegisterForNavigation<ForgetPasswordView, ForgetPasswordViewModel>();

        containerRegistry.RegisterForNavigation<MainChatView, MainChatViewModel>();

        containerRegistry.RegisterForNavigation<Blank>();
    }

    protected override AvaloniaObject CreateShell()
    {
        return null!;
    }

    /// <summary>
    /// 初始化完毕
    /// </summary>
    protected override void OnInitialized()
    {
        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(LoginView));

        // 获取STUN服务器地址
        Container.Resolve<IStunServerManager>().GetStunServersUrl();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var view = Container.Resolve<MainView>();
            singleView.MainView = view;
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var view = Container.Resolve<MainWindow>();
            desktop.MainWindow = view;
        }
    }
}