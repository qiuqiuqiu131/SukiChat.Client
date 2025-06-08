using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Notification;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.About;
using ChatClient.Desktop.Views.CallView;
using ChatClient.Media.CallManager;
using ChatClient.Media.CallOperator;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Org.BouncyCastle.Asn1.X509;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;
using SystemSettingView = ChatClient.Desktop.Views.SystemSetting.SystemSettingView;

namespace ChatClient.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    #region CurrentThemeStyle

    private ThemeStyle _currentThemeStyle;

    public ThemeStyle CurrentThemeStyle
    {
        get => _currentThemeStyle;
        private set => SetProperty(ref _currentThemeStyle, value);
    }

    #endregion

    #region ActiveChatPage

    private ChatPageBase? _activePage;

    public ChatPageBase? ActivePage
    {
        get => _activePage;
        set
        {
            var previousPage = _activePage;
            if (SetProperty(ref _activePage, value))
            {
                previousPage?.OnNavigatedFrom();
                _activePage?.OnNavigatedTo();
                _userManager.CurrentChatPage = ActivePage?.DisplayName ?? "";
            }
        }
    }

    #endregion

    #region User

    private UserDto user;

    public UserDto User
    {
        get => user;
        set => SetProperty(ref user, value);
    }

    #endregion

    private AvaloniaList<GroupFriendDto> GroupFriends => _userManager.GroupFriends;
    private AvaloniaList<GroupGroupDto> GroupGroups => _userManager.GroupGroups;

    public AvaloniaList<ChatPageBase> ChatPages { get; private set; }

    public ISukiDialogManager SukiDialogManager { get; private set; }

    public INotificationMessageManager NotificationMessageManager { get; init; } = new NotificationMessageManager();

    public AsyncDelegateCommand ExitCommnad { get; init; }
    public DelegateCommand ShowSystemSettingCommand { get; init; }
    public DelegateCommand ShowAboutCommand { get; init; }

    public MainWindowViewModel(IEnumerable<ChatPageBase> chatPages,
        IThemeStyle themeStyle,
        IConnection connection,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        ISukiDialogManager sukiDialogManager,
        IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        _containerProvider = containerProvider;
        _userManager = userManager;

        SukiDialogManager = sukiDialogManager;

        ChatPages = new AvaloniaList<ChatPageBase>(chatPages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));

        User = userManager.User?.UserDto!;
        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        ExitCommnad = new AsyncDelegateCommand(TryExit);
        ShowSystemSettingCommand = new DelegateCommand(ShowSystemSetting);
        ShowAboutCommand = new DelegateCommand(ShowAbout);

        RegisterEvent();
        RegisterDtoEvent();
    }

    private void ShowAbout()
    {
        _dialogService.Show(nameof(AboutView));
    }

    private void ShowSystemSetting()
    {
        _dialogService.Show(nameof(SystemSettingView));
    }

    #region RelationDtoUpdate

    private void RegisterDtoEvent()
    {
        foreach (var groupFriendDto in GroupFriends)
        {
            groupFriendDto.Friends.CollectionChanged += OnRelationCollectionChanged;
            foreach (var friend in groupFriendDto.Friends)
            {
                friend.OnFriendRelationChanged += FriendRelationDtoOnOnFriendRelationChanged;
                friend.OnGroupingChanged += FriendRelationDtoOnOnGroupingChanged;
            }
        }

        foreach (var groupGroupDto in GroupGroups)
        {
            groupGroupDto.Groups.CollectionChanged += OnRelationCollectionChanged;
            foreach (var group in groupGroupDto.Groups)
            {
                group.OnGroupRelationChanged += GroupRelationDtoOnOnGroupRelationChanged;
                group.OnGroupingChanged += GroupRelationDtoOnOnGroupingChanged;
            }
        }

        GroupFriends.CollectionChanged += OnCollectionChanged;
        GroupGroups.CollectionChanged += OnCollectionChanged;
    }

    private void UnRegisterDtoEvent()
    {
        GroupFriends.CollectionChanged -= OnCollectionChanged;
        GroupGroups.CollectionChanged -= OnCollectionChanged;

        foreach (var groupFriendDto in GroupFriends)
        {
            groupFriendDto.Friends.CollectionChanged -= OnRelationCollectionChanged;
            foreach (var friend in groupFriendDto.Friends)
            {
                friend.OnFriendRelationChanged -= FriendRelationDtoOnOnFriendRelationChanged;
                friend.OnGroupingChanged -= FriendRelationDtoOnOnGroupingChanged;
            }
        }

        foreach (var groupGroupDto in GroupGroups)
        {
            groupGroupDto.Groups.CollectionChanged -= OnRelationCollectionChanged;
            foreach (var group in groupGroupDto.Groups)
            {
                group.OnGroupRelationChanged -= GroupRelationDtoOnOnGroupRelationChanged;
                group.OnGroupingChanged -= GroupRelationDtoOnOnGroupingChanged;
            }
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems)
            {
                if (item is GroupFriendDto groupFriendDto)
                {
                    groupFriendDto.Friends.CollectionChanged += OnRelationCollectionChanged;
                }
                else if (item is GroupGroupDto groupGroupDto)
                {
                    groupGroupDto.Groups.CollectionChanged += OnRelationCollectionChanged;
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var item in e.OldItems)
            {
                if (item is GroupFriendDto groupFriendDto)
                {
                    groupFriendDto.Friends.CollectionChanged -= OnRelationCollectionChanged;
                }
                else if (item is GroupGroupDto groupGroupDto)
                {
                    groupGroupDto.Groups.CollectionChanged -= OnRelationCollectionChanged;
                }
            }
        }
    }

    private void OnRelationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems)
            {
                if (item is FriendRelationDto friendRelationDto)
                {
                    friendRelationDto.OnFriendRelationChanged += FriendRelationDtoOnOnFriendRelationChanged;
                    friendRelationDto.OnGroupingChanged += FriendRelationDtoOnOnGroupingChanged;
                }
                else if (item is GroupRelationDto groupRelationDto)
                {
                    groupRelationDto.OnGroupRelationChanged += GroupRelationDtoOnOnGroupRelationChanged;
                    groupRelationDto.OnGroupingChanged += GroupRelationDtoOnOnGroupingChanged;
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var item in e.OldItems)
            {
                if (item is FriendRelationDto friendRelationDto)
                {
                    friendRelationDto.OnFriendRelationChanged -= FriendRelationDtoOnOnFriendRelationChanged;
                    friendRelationDto.OnGroupingChanged -= FriendRelationDtoOnOnGroupingChanged;
                }
                else if (item is GroupRelationDto groupRelationDto)
                {
                    groupRelationDto.OnGroupRelationChanged -= GroupRelationDtoOnOnGroupRelationChanged;
                    groupRelationDto.OnGroupingChanged -= GroupRelationDtoOnOnGroupingChanged;
                }
            }
        }
    }

    private async void GroupRelationDtoOnOnGroupingChanged(GroupRelationDto obj, string origionName)
    {
        var groupList1 = _userManager.GroupGroups?.FirstOrDefault(d => d.GroupName.Equals(origionName));
        if (groupList1 != null)
            groupList1.Groups.Remove(obj);

        var groupList2 = _userManager.GroupGroups?.FirstOrDefault(d => d.GroupName.Equals(obj.Grouping));
        if (groupList2 != null)
            groupList2.Groups.Add(obj);

        await Task.Delay(50);

        _eventAggregator.GetEvent<GroupRelationSelectEvent>().Publish(obj);
    }

    private async void FriendRelationDtoOnOnGroupingChanged(FriendRelationDto obj, string origionName)
    {
        var groupList1 = _userManager.GroupFriends?.FirstOrDefault(d => d.GroupName.Equals(origionName));
        if (groupList1 != null)
            groupList1.Friends.Remove(obj);

        var groupList2 = _userManager.GroupFriends?.FirstOrDefault(d => d.GroupName.Equals(obj.Grouping));
        if (groupList2 != null)
            groupList2.Friends.Add(obj);

        await Task.Delay(50);

        _eventAggregator.GetEvent<FriendRelationSelectEvent>().Publish(obj);
    }

    private void GroupRelationDtoOnOnGroupRelationChanged(GroupRelationDto obj)
    {
        Task.Run(async () =>
        {
            var groupService = _containerProvider.Resolve<IGroupService>();
            var result = await groupService.UpdateGroupRelation(_userManager.User!.Id, obj);
        });
    }

    private void FriendRelationDtoOnOnFriendRelationChanged(FriendRelationDto obj)
    {
        Task.Run(async () =>
        {
            var friendService = _containerProvider.Resolve<IFriendService>();
            var result = await friendService.UpdateFriendRelation(_userManager.User!.Id, obj);
        });
    }

    #endregion

    #region RegisterEvent

    private List<SubscriptionToken> tokens = new();

    private void RegisterEvent()
    {
        var token1 = _eventAggregator.GetEvent<ResponseEvent<LogoutCommand>>().Subscribe(d =>
        {
            if (User.Id.Equals(d.Id))
            {
                Dispatcher.UIThread.Invoke(() => { ForceToExit("您的账号在其他设备上登录！"); });
            }
        });
        tokens.Add(token1);

        var token2 = _eventAggregator.GetEvent<ResponseEvent<FriendRequestFromServer>>().Subscribe(d =>
        {
            var i = d;
        });
        tokens.Add(token2);

        var token3 = _eventAggregator.GetEvent<ChangePageEvent>().Subscribe(d =>
        {
            ActivePage = ChatPages.FirstOrDefault(x => x.DisplayName.Equals(d.PageName));
        });
        tokens.Add(token3);

        var token4 = _eventAggregator.GetEvent<NotificationEvent>().Subscribe(d =>
        {
            NotificationMessageManager.ShowMessage(d.Message, d.Type, TimeSpan.FromSeconds(2.5));
        });
        tokens.Add(token4);

        var token5 = _eventAggregator.GetEvent<ChatPageUnreadCountChangedEvent>().Subscribe(d =>
        {
            var page = ChatPages.FirstOrDefault(p => p.DisplayName.Equals(d.Item1));
            if (page != null)
                page.UnReadMessageCount = d.Item2;
        });
        tokens.Add(token5);

        var token6 = _eventAggregator.GetEvent<ResponseEvent<CallRequest>>().Subscribe(async d =>
        {
            var callManager = _containerProvider.Resolve<ICallManager>();
            var callOperator = await callManager.GetCallRequest(d);

            if (callOperator == null)
                return;

            ChatCallHelper.CloseOtherCallDialog();

            var dialogService = _containerProvider.Resolve<IDialogService>();
            var parameter = new DialogParameters
            {
                { "callOperator", callOperator },
                { "request", d }
            };
            Dispatcher.UIThread.Post(() =>
            {
                if (callOperator is TelephoneCallOperator)
                    dialogService.Show(nameof(CallView), parameter, null, nameof(SukiCallDialogWindow));
                else if (callOperator is VideoCallOperator)
                    dialogService.Show(nameof(VideoCallView), parameter, null, nameof(SukiCallDialogWindow));
            });
        });
        tokens.Add(token6);

        var token7 = _eventAggregator.GetEvent<CallOver>().Subscribe(d =>
        {
            Task.Run(async () =>
            {
                var chatService = _containerProvider.Resolve<IChatService>();
                var chatMessageDto = new ChatMessageDto { Content = d, Type = ChatMessage.ContentOneofCase.CallMess };
                var (state, chatId) =
                    await chatService.SendChatMessage(_userManager.User.Id, d.targetId, [chatMessageDto]);
            });
        });
        tokens.Add(token7);

        IsConnected.ConnecttedChanged += ConnectionChanged;
    }

    private void UnRegisterEvent()
    {
        foreach (var token in tokens)
            token.Dispose();
        tokens.Clear();
        IsConnected.ConnecttedChanged -= ConnectionChanged;
    }

    private void ConnectionChanged(bool connected)
    {
        if (!connected)
        {
            Dispatcher.UIThread.Invoke(() => { ForceToExit("服务器断开连接！"); });
        }
    }

    #endregion

    #region ViewLogic

    private async Task ForceToExit(string Content)
    {
        async void ForceToExitCallback(IDialogResult dialogResult)
        {
            if (dialogResult.Result == ButtonResult.OK)
                await Exit();
        }

        // TranslateWindowHelper.CloseAllDialog();
        // SukiDialogManager.DismissDialog();
        // SukiDialogManager.CreateDialog()
        //     .WithViewModel(d => new WarningDialogViewModel(d, "登录异常", Content, ForceToExitCallback))
        //     .TryShow();
    }

    private async Task TryExit()
    {
        async void TryExitCallback(IDialogResult dialogResult)
        {
            if (dialogResult.Result == ButtonResult.OK)
                await Exit();
        }

        SukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定要退出登录吗？", TryExitCallback))
            .TryShow();
    }

    /// <summary>
    /// 退回到登录界面
    /// </summary>
    private async Task Exit()
    {
        TranslateWindowHelper.TranslateToLoginWindow();
        // 退出登录
        await _userManager.Logout();
    }

    #endregion

    #region Dispose

    private bool _isDisposed = false;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // 释放托管资源
                try
                {
                    UnRegisterDtoEvent();
                    UnRegisterEvent();
                    ChatPages?.Clear();
                }
                catch (Exception e)
                {
                    // Console.WriteLine("Dispose error: " + e);
                }
            }

            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数，作为安全网
    /// </summary>
    ~MainWindowViewModel()
    {
        Dispose(false);
    }

    #endregion
}