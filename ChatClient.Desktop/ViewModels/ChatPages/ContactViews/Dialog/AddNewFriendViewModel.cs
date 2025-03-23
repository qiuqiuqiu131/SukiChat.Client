using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class AddNewFriendViewModel : ViewModelBase, IDialogAware
{
    private string? id;

    public string? Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string? group;

    public string? Group
    {
        get => group;
        set => SetProperty(ref group, value);
    }

    public DelegateCommand SendFriendRequestCommand { get; }
    public DelegateCommand SendGroupRequestCommand { get; }

    public ThemeStyle CurrentThemeStyle { get; }

    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly ISukiToastManager _toastManager;

    public AddNewFriendViewModel(IContainerProvider containerProvider,
        IThemeStyle themeStyle,
        IUserManager userManager,
        ISukiToastManager toastManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        _toastManager = toastManager;

        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        SendFriendRequestCommand = new DelegateCommand(SendFriendRequest);
        SendGroupRequestCommand = new DelegateCommand(SendGroupRequest);
    }

    private async void SendFriendRequest()
    {
        if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Group))
            return;

        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) = await _friendService.AddFriend(_userManager.User!.Id, Id, Group);
        if (state)
            _toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Success)
                .WithTitle("好友请求成功")
                .WithContent("耐心等待对方同意")
                .Queue();
        else
            _toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Error)
                .WithTitle("好友请求失败")
                .WithContent(message)
                .Queue();
    }

    private async void SendGroupRequest()
    {
        if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Group))
            return;

        var _groupService = _containerProvider.Resolve<IGroupService>();
        var (state, message) = await _groupService.JoinGroupRequest(_userManager.User!.Id, Id);
        if (state)
            _toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Success)
                .WithTitle("好友请求成功")
                .WithContent("耐心等待对方同意")
                .Queue();
        else
            _toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Error)
                .WithTitle("好友请求失败")
                .WithContent(message)
                .Queue();
    }

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}