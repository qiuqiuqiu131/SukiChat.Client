using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService;
using ChatClient.BaseService.Helper;
using ChatClient.Client;
using ChatClient.DataBase;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.ViewModels.ChatPages;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.Login;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.ChatPages;
using ChatClient.Desktop.Views.ChatPages.ChatViews;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ContactDetailView;
using ChatClient.Desktop.Views.Login;
using ChatClient.Resources;
using ChatClient.Tool.Common;
using ChatClient.Tool.UIEntity;
using Microsoft.Extensions.Configuration;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop;

public class App : PrismApplication
{
    public new static PrismApplication Current => (PrismApplication)Application.Current;

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
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="containerRegistry"></param>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册配置文件
        IConfigurationBuilder configurationBuilder =
            new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        containerRegistry.RegisterInstance(configurationBuilder.Build());

        // 注册数据库
        containerRegistry.RegisterDataBase();

        // 注册服务
        containerRegistry.RegisterBaseServices();

        // 注册SukiManager
        containerRegistry.RegisterSingleton<ISukiToastManager, SukiToastManager>();
        containerRegistry.RegisterSingleton<ISukiDialogManager, SukiDialogManager>();

        // 注册导航View
        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
        containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
        containerRegistry.RegisterForNavigation<FriendRequestView, FriendRequestViewModel>();
        containerRegistry.RegisterForNavigation<FriendDetailView, FriendDetailViewModel>();
        containerRegistry.RegisterForNavigation<ChatEmptyView>();
        containerRegistry.RegisterForNavigation<ChatFriendPanelView, ChatFriendPanelViewModel>();
        containerRegistry.RegisterForNavigation<ChatGroupPanelView, ChatGroupPanelViewModel>();

        // 注册DialogView
        containerRegistry.RegisterDialogWindow<SukiDialogWindow>();
        containerRegistry.RegisterDialog<CreateGroupView, CreateGroupViewModel>();

        var views = ConfigureViews(containerRegistry);
        DataTemplates.Add(new ViewLocator(views));
    }

    protected override AvaloniaObject CreateShell()
    {
        return null;
    }

    /// <summary>
    /// 初始化完毕
    /// </summary>
    protected async override void OnInitialized()
    {
        IRegionManager regionManager = Container.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion(RegionNames.LoginRegion, typeof(LoginView));
        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatEmptyView));
        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatFriendPanelView));
        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatGroupPanelView));

        // ProtoFileIOHelper helper = Container.Resolve<ProtoFileIOHelper>();
        // byte[] bytes;
        // using (FileStream fileStream = new FileStream("D:\\ChatResources\\1.png", FileMode.Open, FileAccess.Read))
        // {
        //     bytes = new byte[fileStream.Length];
        //     await fileStream.ReadAsync(bytes, 0, bytes.Length);
        // }
        //
        // var file = await helper.UploadFileAsync("Users", "test.png", bytes);
    }


    /// <summary>
    /// 注册SukiChatViews
    /// 使用Model优先原则
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private SukiChatViews ConfigureViews(IContainerRegistry services)
    {
        return new SukiChatViews()
            .AddView<UserOptionView, UserOptionsViewModel>(services)
            .AddView<ChatView, ChatViewModel>(services)
            .AddView<ContactsView, ContactsViewModel>(services)
            .AddView<ThemeView, ThemeViewModel>(services);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Container.Resolve<LoginWindowView>();
        }
    }
}