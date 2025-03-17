using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Events;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
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

    public AvaloniaList<ChatPageBase> ChatPages { get; private set; }

    public ISukiToastManager ToastManager { get; init; }
    public ISukiDialogManager DialogManager { get; init; }

    public DelegateCommand ExitCommnad { get; init; }

    public MainWindowViewModel(IEnumerable<ChatPageBase> chatPages,
        IThemeStyle themeStyle,
        IConnection connection,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager,
        IEventAggregator eventAggregator,
        IUserManager userManager)
    {
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        ToastManager = toastManager;
        DialogManager = dialogManager;

        ChatPages = new AvaloniaList<ChatPageBase>(chatPages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));

        User = userManager.User!;
        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        ExitCommnad = new DelegateCommand(TryExit);

        RegisterEvent();
    }

    #region RegisterEvent

    private List<SubscriptionToken> tokens = new();

    private void RegisterEvent()
    {
        var token1 = _eventAggregator.GetEvent<ResponseEvent<LogoutCommand>>().Subscribe(d =>
        {
            if (User.Id.Equals(d.Id))
            {
                Dispatcher.UIThread.Invoke(() => { ForceToExit("您的账号在其他设备上登录，您被迫下线。"); });
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

        IsConnected.ConnecttedChanged += ConnectionChanged;
        CurrentThemeStyle.ThemeStyleChanged += ThemeStyleChanged;
    }

    private void UnRegisterEvent()
    {
        foreach (var token in tokens)
            token.Dispose();
        tokens.Clear();
        IsConnected.ConnecttedChanged -= ConnectionChanged;
        CurrentThemeStyle.ThemeStyleChanged -= ThemeStyleChanged;
    }

    private void ConnectionChanged(bool connected)
    {
        if (!connected)
        {
            Dispatcher.UIThread.Invoke(() => { ForceToExit("与服务器断开连接，您被迫下线。"); });
        }
    }

    private void ThemeStyleChanged((string, string) variant)
    {
        ToastManager.CreateSimpleInfoToast()
            .WithTitle("主题更改")
            .WithContent($"{variant.Item1}更改为{variant.Item2}")
            .Queue();
    }

    #endregion

    private void ForceToExit(string Content)
    {
        DialogManager.CreateDialog()
            .OfType(NotificationType.Warning)
            .WithTitle("退出登录")
            .WithContent(Content)
            .WithActionButton("确定", _ => Exit(), true)
            .TryShow();
    }

    private void TryExit()
    {
        DialogManager.CreateDialog()
            .WithTitle("退出登录")
            .WithContent("您确定要退出SukiChat吗？退出后，需要重新登录。")
            .WithActionButton("退出", _ => { Exit(); }, true)
            .WithActionButton("取消", _ => { }, true)
            .Dismiss().ByClickingBackground()
            .TryShow();
    }

    /// <summary>
    /// 退回到登录界面
    /// </summary>
    private void Exit()
    {
        // 退出登录
        _userManager.Logout();

        TranslateWindowHelper.TranslateToLoginWindow();
    }

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