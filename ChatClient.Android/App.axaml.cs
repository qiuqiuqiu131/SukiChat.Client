using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatClient.Android.Tool;
using ChatClient.Android.ViewModels;
using ChatClient.Android.Views;
using ChatClient.BaseService.SqlSugar;
using ChatClient.DataBase.SqlSugar.SugarDB;
using ChatClient.Resources;
using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;
using Microsoft.Extensions.Configuration;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using SocketClient;
using System.IO;
using Application = Android.App.Application;

namespace ChatClient.Android;

public class App : PrismApplication
{
    public new static PrismApplication Current => (PrismApplication)Avalonia.Application.Current;

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
        FileTool.CopyAssetsToFilesIfNeeded(
        [
            "appsettings.json",
            "appsettings.webrtc.json",
        ]);

        // 注册配置文件
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Application.Context.FilesDir.AbsolutePath,"appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(Application.Context.FilesDir.AbsolutePath, "appsettings.webrtc.json"), optional: false, reloadOnChange: true)
            .Build();

        var appsettings = new AppSettings();
        configuration.Bind(appsettings);
        containerRegistry.RegisterInstance(appsettings);

        // 注册数据库
        containerRegistry.RegisterSugarDataBase();

        containerRegistry.Register<LoginView>()
            .Register<LoginViewModel>();
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
        // 获取STUN服务器地址
        Container.Resolve<IStunServerManager>().GetStunServersUrl();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var view = Container.Resolve<LoginView>();
            var model = Container.Resolve<LoginViewModel>();
            view.DataContext = model;

            singleView.MainView = view;
        }
    }
}