using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ChatClient.Avalonia.Common;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.SqlSugar;
using ChatClient.DataBase.SqlSugar.SugarDB;
using ChatClient.Desktop.CornerDialog;
using ChatClient.Desktop.Services;
using ChatClient.Desktop.Suki;
using ChatClient.Desktop.ViewModels;
using ChatClient.Desktop.ViewModels.About;
using ChatClient.Desktop.ViewModels.CallViewModel;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Dialog;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.SideRegion;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;
using ChatClient.Desktop.ViewModels.LocalSearchUserGroupView;
using ChatClient.Desktop.ViewModels.LocalSearchUserGroupView.Region;
using ChatClient.Desktop.ViewModels.Login;
using ChatClient.Desktop.ViewModels.SearchUserGroup;
using ChatClient.Desktop.ViewModels.SearchUserGroup.Region;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Desktop.ViewModels.SystemSetting;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.About;
using ChatClient.Desktop.Views.CallView;
using ChatClient.Desktop.Views.ChatPages.ChatViews;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ChatViews.Dialog;
using ChatClient.Desktop.Views.ChatPages.ChatViews.SideRegion;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Region;
using ChatClient.Desktop.Views.LocalSearchUserGroupView;
using ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;
using ChatClient.Desktop.Views.Login;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Desktop.Views.SearchUserGroupView.Region;
using ChatClient.Desktop.Views.ShareView;
using ChatClient.Desktop.Views.SukiDialog;
using ChatClient.Desktop.Views.SystemSetting;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Media.Desktop;
using ChatClient.Resources;
using ChatClient.Tool.Config;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SIPSorcery;
using SocketClient;
using SqlSugar;
using SukiUI.Dialogs;
using ForgetPasswordView = ChatClient.Desktop.Views.ForgetPassword.ForgetPasswordView;
using RegisterView = ChatClient.Desktop.Views.Register.RegisterView;

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
        moduleCatalog.AddModule<MediaModule>();
        moduleCatalog.AddModule<SqlSugarServiceModule>();
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="containerRegistry"></param>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册配置文件
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.webrtc.json", optional: false, reloadOnChange: true)
            .Build();

        var appsettings = new AppSettings();
        configuration.Bind(appsettings);
        containerRegistry.RegisterInstance(appsettings);

        // 注册Logger
        ILoggerFactory loggerFactory = LoggerFactory.Create(build =>
        {
            build.AddConsole()
                .AddConfiguration(configuration);
        });
        containerRegistry.RegisterInstance(loggerFactory);

        // 注册数据库
        containerRegistry.RegisterSugarDataBase(); // SqlSugar 数据库

        // 注册SukiUI相关服务
        containerRegistry.RegisterSingleton<IThemeStyle, ThemeStyleManager>();

        // 注册持久化文件保存路径
        containerRegistry.RegisterSingleton<IAppDataManager, DesktopAppDataManager>();

        containerRegistry.Register<MainWindowView>()
            .Register<MainWindowViewModel>();
        containerRegistry.Register<LoginWindowView>()
            .Register<LoginWindowViewModel>();

        containerRegistry.RegisterSingleton<ISukiDialogManager, SukiDialogManager>();
        containerRegistry.Register<ICornerDialogService, CornerDialogService>();

        containerRegistry.Register<CornerWindow>();

        // 登录
        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
        containerRegistry.RegisterForNavigation<LoginSettingView, LoginSettingViewModel>();
        containerRegistry.RegisterForNavigation<NetSettingView, NetSettingViewModel>();
        // 主页面
        containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
        containerRegistry.RegisterForNavigation<ContactsView, ContactsViewModel>();
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
        // 聊天侧边栏
        containerRegistry.RegisterForNavigation<FriendSideView, FriendSideViewModel>();
        containerRegistry.RegisterForNavigation<GroupSideView, GroupSideViewModel>();
        containerRegistry.RegisterForNavigation<GroupSideEditView, GroupSideEditViewModel>();
        containerRegistry.RegisterForNavigation<GroupSideMemberView, GroupSideMemberViewModel>();
        // 搜索用户和群
        containerRegistry.RegisterForNavigation<SearchFriendView, SearchFriendViewModel>();
        containerRegistry.RegisterForNavigation<SearchGroupView, SearchGroupViewModel>();
        containerRegistry.RegisterForNavigation<SearchAllView, SearchAllViewModel>();
        // 本地搜索
        containerRegistry.RegisterForNavigation<LocalSearchAllView, LocalSearchAllViewModel>();
        containerRegistry.RegisterForNavigation<LocalSearchUserView, LocalSearchUserViewModel>();
        containerRegistry.RegisterForNavigation<LocalSearchGroupView, LocalSearchGroupViewModel>();
        // 设置
        containerRegistry.RegisterForNavigation<ThemeView, ThemeViewModel>();
        containerRegistry.RegisterForNavigation<AccountView, AccountViewModel>();
        containerRegistry.RegisterForNavigation<UserSettingView, UserSettingViewModel>();
        containerRegistry.RegisterForNavigation<UndoView>();

        // 注册DialogView
        containerRegistry.RegisterDialogWindow<SukiCallDialogWindow>(nameof(SukiCallDialogWindow));
        containerRegistry.RegisterDialogWindow<SukiChatDialogWindow>(nameof(SukiChatDialogWindow));
        containerRegistry.RegisterDialogWindow<SukiDialogWindow>();

        // containerRegistry.RegisterDialog<NetSettingView, NetSettingViewModel>();
        containerRegistry.RegisterDialog<ForgetPasswordView, ForgetPasswordViewModel>();
        containerRegistry.RegisterDialog<RegisterView, RegisterViewModel>();
        // 聊天窗口
        containerRegistry.RegisterDialog<ChatFriendDialogView, ChatFriendDialogViewModel>();
        containerRegistry.RegisterDialog<ChatGroupDialogView, ChatGroupDialogViewModel>();
        // 搜索用户和群
        containerRegistry.RegisterDialog<SearchUserGroupView, SearchUserGroupViewModel>();
        // 本地搜索
        containerRegistry.RegisterDialog<LocalSearchUserGroupView, LocalSearchUserGroupViewModel>();
        // 设置
        containerRegistry.RegisterDialog<SystemSettingView, SystemSettingViewModel>();
        // 添加关系
        containerRegistry.RegisterDialog<AddFriendRequestView, AddFriendRequestViewModel>();
        containerRegistry.RegisterDialog<AddGroupRequestView, AddGroupRequestViewModel>();
        // 头像编辑
        containerRegistry.RegisterDialog<UserHeadEditView, UserHeadEditViewModel>();
        // 通话
        containerRegistry.RegisterDialog<CallView, CallViewModel>();
        containerRegistry.RegisterDialog<VideoCallView, VideoCallViewModel>();
        // 关于
        containerRegistry.RegisterDialog<AboutView, AboutViewModel>();

        // 注册边角对话框
        containerRegistry.Register<ICornerDialogWindow, CornerWindow>();
        containerRegistry.RegisterDialog<FriendChatMessageBoxView, FriendChatMessageBoxViewModel>();
        containerRegistry.RegisterDialog<GroupChatMessageBoxView, GroupChatMessageBoxViewModel>();

        var views = ConfigureViews(containerRegistry);
        DataTemplates.Add(new ViewLocator(views));
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
        StaticConfig.EnableAot = true;

        var appSettings = Container.Resolve<AppSettings>();
        if (appSettings.SIPSorceryDebug)
        {
            ILoggerFactory loggerFactory = Container.Resolve<ILoggerFactory>();
            LogFactory.Set(loggerFactory);
        }

        Container.EnsureDbCreated();

        // 设置Region默认视图
        IRegionManager regionManager = Container.Resolve<IRegionManager>();

        regionManager.RegisterViewWithRegion(RegionNames.LoginRegion, nameof(LoginView));

        regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(ChatView));

        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatEmptyView));
        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatFriendPanelView));
        regionManager.RegisterViewWithRegion(RegionNames.ChatRightRegion, typeof(ChatGroupPanelView));

        regionManager.RegisterViewWithRegion(RegionNames.ContactsRegion, typeof(ChatEmptyView));

        regionManager.RegisterViewWithRegion(RegionNames.SystemSettingRegion, nameof(ThemeView));

        // 获取STUN服务器地址
        Container.Resolve<IStunServerManager>().GetStunServersUrl();

        // 加载主题样式
        Container.Resolve<IThemeStyle>();
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
            // 同意添加好友
            .AddView<AcceptFriendView, AcceptFriendViewModel>(services)
            // 编辑用户信息
            .AddView<EditUserDataView, EditUserDataViewModel>(services)
            // 分享
            .AddView<ShareView, ShareViewModel>(services)
            // 编辑分组
            .AddView<AddGroupView, AddGroupViewModel>(services)
            .AddView<RenameGroupView, RenameGroupViewModel>(services)
            .AddView<DeleteGroupView, DeleteGroupViewModel>(services)
            // 创建群聊
            .AddView<CreateGroupView, CreateGroupViewModel>(services)
            // 移除群成员
            .AddView<RemoveGroupMemberView, RemoveGroupMemberViewModel>(services)
            // 群聊头像编辑
            .AddView<EditGroupHeadView, EditGroupHeadViewModel>(services)
            // 通用Dialog
            .AddView<CommonDialogView, CommonDialogViewModel>(services)
            .AddView<WarningDialogView, WarningDialogViewModel>(services)
            // 通用Dialog
            .AddView<SukiDialogView, SukiDialogViewModel>(services)
            // 编辑密码
            .AddView<EditPasswordView, EditPasswordViewModel>(services)
            // 发送文件dialog
            .AddView<SendFileDialogView, SendFileDialogViewModel>(services);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = Container.Resolve<LoginWindowView>();
            desktop.MainWindow = window;
            var ico = AssetLoader.Open(new Uri("avares://ChatClient.Desktop/Assets/Icon.ico"));
            window.Icon = new WindowIcon(ico);
            window.Show();
        }
    }
}