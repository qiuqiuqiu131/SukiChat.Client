using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class AddFriendRequestViewModel : BindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    private UserDto _userDto;

    public UserDto UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string? _message;

    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private string? _remark;

    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    private string? _group;

    public string? Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    private List<string> _groups;

    public List<string> Groups
    {
        get => _groups;
        set => SetProperty(ref _groups, value);
    }

    private INotificationMessageManager? notificationManager;

    public DelegateCommand SendFriendRequestCommand { get; }
    public DelegateCommand CancleCommand { get; }

    private bool isSended = false;

    public AddFriendRequestViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        Groups = _userManager.GroupFriends!.Select(d => d.GroupName).Order().ToList();

        SendFriendRequestCommand = new DelegateCommand(SendFriendRequest);
        CancleCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    private async void SendFriendRequest()
    {
        if (isSended) return;

        isSended = true;

        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) =
            await _friendService.AddFriend(_userManager.User!.Id, _userDto.Id, Remark ?? "", Group ?? "默认分组",
                Message ?? "");
        if (state)
            notificationManager?.ShowMessage("好友请求已发送", NotificationType.Success, TimeSpan.FromSeconds(1.5));
        else
            notificationManager?.ShowMessage("好友请求失败", NotificationType.Error, TimeSpan.FromSeconds(1.5));

        RequestClose.Invoke();
    }

    #region MyRegion

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        notificationManager = null;
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        notificationManager = parameters.GetValue<INotificationMessageManager>("notificationManager");
        UserDto = parameters.GetValue<UserDto>("UserDto");
        Group = "默认分组";
        Message = "我是" + _userManager.User!.Name;
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}