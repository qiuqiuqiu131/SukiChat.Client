using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.SearchUserGroup;

public class AddGroupRequestViewModel : BindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    private GroupDto _groupDto;

    public GroupDto GroupDto
    {
        get => _groupDto;
        set => SetProperty(ref _groupDto, value);
    }

    private string? _message;

    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public INotificationMessageManager NotificationMessageManager { get; } = new NotificationMessageManager();

    public DelegateCommand SendGroupRequestCommand { get; }
    public DelegateCommand CancleCommand { get; }

    private bool isSended;

    public AddGroupRequestViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        SendGroupRequestCommand = new DelegateCommand(SendGroupRequest);
        CancleCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    private async void SendGroupRequest()
    {
        if (isSended) return;

        isSended = true;

        var _groupService = _containerProvider.Resolve<IGroupService>();
        var (state, message) =
            await _groupService.JoinGroupRequest(_userManager.User!.Id, _groupDto.Id,
                Message ?? "");
        if (state)
            NotificationMessageManager.ShowMessage("入群请求发送成功", NotificationType.Success, TimeSpan.FromSeconds(1.5));
        else
            NotificationMessageManager.ShowMessage("入群请求发送失败", NotificationType.Error, TimeSpan.FromSeconds(1.5));
        await Task.Delay(TimeSpan.FromSeconds(0.9));
        RequestClose.Invoke();
    }

    #region MyRegion

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        GroupDto = parameters.GetValue<GroupDto>("GroupDto");
        Message = "我是" + _userManager.User!.Name;
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}