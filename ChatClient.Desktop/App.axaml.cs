using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService;
using ChatClient.Client;
using ChatClient.DataBase;
using ChatClient.Desktop.ViewModels;
using ChatClient.Desktop.ViewModels.ChatPages;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;
using ChatClient.Desktop.ViewModels.Login;
using ChatClient.Desktop.ViewModels.SearchUserGroup;
using ChatClient.Desktop.ViewModels.SearchUserGroup.Region;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.ChatPages;
using ChatClient.Desktop.Views.ChatPages.ChatViews;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Region;
using ChatClient.Desktop.Views.Login;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Desktop.Views.SearchUserGroupView.Region;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Resources;
using ChatClient.Tool.Common;
using ChatClient.Tool.UIEntity;
using Microsoft.Extensions.Configuration;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

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

        containerRegistry.Register<MainWindowView>()
            .Register<MainWindowViewModel>();
        containerRegistry.Register<LoginWindowView>().Register<LoginWindowViewModel>();

        // 注册导航View
        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
        containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
        // 用户和群聊通知面板
        containerRegistry.RegisterForNavigation<FriendRequestView, FriendRequestViewModel>();
        containerRegistry.RegisterForNavigation<GroupRequestView, GroupRequestViewModel>();
        // 用户和群聊信息面板
        containerRegistry.RegisterForNavigation<FriendDetailView, FriendDetailViewModel>();
        containerRegistry.RegisterForNavigation<GroupDetailView, GroupDetailViewModel>();
        // 聊天面板
        containerRegistry.RegisterForNavigation<ChatEmptyView>();
        containerRegistry.RegisterForNavigation<ChatFriendPanelView, ChatFriendPanelViewModel>();
        containerRegistry.RegisterForNavigation<ChatGroupPanelView, ChatGroupPanelViewModel>();
        // 搜索用户和群
        containerRegistry.RegisterForNavigation<SearchFriendView, SearchFriendViewModel>();
        containerRegistry.RegisterForNavigation<SearchGroupView, SearchGroupViewModel>();
        containerRegistry.RegisterForNavigation<SearchAllView, SearchAllViewModel>();

        // 注册DialogView
        containerRegistry.RegisterDialogWindow<SukiDialogWindow>();
        containerRegistry.RegisterDialog<RegisterWindowView, RegisterWindowViewModel>();

        containerRegistry.RegisterDialog<CreateGroupView, CreateGroupViewModel>();
        containerRegistry.RegisterDialog<SearchUserGroupView, SearchUserGroupViewModel>();
        // 通用Dialog
        containerRegistry.RegisterDialog<CommonDialogView, CommonDialogViewModel>();
        containerRegistry.RegisterDialog<WarningDialogView, WarningDialogViewModel>();
        // 编辑分组
        containerRegistry.RegisterDialog<AddGroupView, AddGroupViewModel>();
        containerRegistry.RegisterDialog<RenameGroupView, RenameGroupViewModel>();
        containerRegistry.RegisterDialog<DeleteGroupView, DeleteGroupViewModel>();
        // 添加关系
        containerRegistry.RegisterDialog<AddFriendRequestView, AddFriendRequestViewModel>();
        containerRegistry.RegisterDialog<AddGroupRequestView, AddGroupRequestViewModel>();
        // 头像编辑
        containerRegistry.RegisterDialog<UserHeadEditView, UserHeadEditViewModel>();
        // 编辑用户信息
        containerRegistry.RegisterDialog<EditUserDataView, EditUserDataViewModel>();

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

        regionManager.RegisterViewWithRegion(RegionNames.ContactsRegion, typeof(ChatEmptyView));
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
            .AddView<ThemeView, ThemeViewModel>(services)
            .AddView<SukiDialogView, SukiDialogViewModel>(services);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Container.Resolve<LoginWindowView>();
        }
    }
}