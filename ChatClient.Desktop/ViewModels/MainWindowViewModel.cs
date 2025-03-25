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
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;
using SukiUI.Toasts;

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

    public INotificationMessageManager NotificationMessageManager { get; init; } = new NotificationMessageManager();

    public AsyncDelegateCommand ExitCommnad { get; init; }

    public MainWindowViewModel(IEnumerable<ChatPageBase> chatPages,
        IThemeStyle themeStyle,
        IConnection connection,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        _containerProvider = containerProvider;
        _userManager = userManager;

        ChatPages = new AvaloniaList<ChatPageBase>(chatPages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));

        User = userManager.User!;
        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        ExitCommnad = new AsyncDelegateCommand(TryExit);

        RegisterEvent();
        RegisterDtoEvent();
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
            NotificationMessageManager.ShowMessage(d.Message, d.Type, TimeSpan.FromSeconds(1.5));
        });
        tokens.Add(token4);

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
        var result = await _dialogService.ShowDialogAsync(nameof(WarningDialogView),
            new DialogParameters { { "message", Content }, { "title", "登录异常" } });
        if (result.Result != ButtonResult.OK) return;

        await Exit();
    }

    private async Task TryExit()
    {
        var result = await _dialogService.ShowDialogAsync(nameof(CommonDialogView),
            new DialogParameters { { "message", "确定退出登录吗？" } });
        if (result.Result != ButtonResult.OK) return;

        await Exit();
    }

    /// <summary>
    /// 退回到登录界面
    /// </summary>
    private async Task Exit()
    {
        // 退出登录
        await _userManager.Logout();
        _ = Task.Run(() => { TranslateWindowHelper.TranslateToLoginWindow(); });
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
                UnRegisterEvent();
                try
                {
                    ChatPages?.Clear();
                }
                catch (Exception e)
                {
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