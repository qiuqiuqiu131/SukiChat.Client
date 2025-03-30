using System;
using System.Collections.Generic;
using System.Linq;
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

    private string? nickName;

    public string? NickName
    {
        get => nickName;
        set => SetProperty(ref nickName, value);
    }

    private List<string> _groups;

    public List<string> Groups
    {
        get => _groups;
        set => SetProperty(ref _groups, value);
    }

    private INotificationMessageManager? notificationManager;

    public DelegateCommand SendGroupRequestCommand { get; }
    public DelegateCommand CancleCommand { get; }

    private bool isSended;

    public AddGroupRequestViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        Groups = _userManager.GroupGroups!.Select(d => d.GroupName).Order().ToList();

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
                Message ?? "", NickName ?? "", Group ?? "默认分组", Remark ?? "");
        if (state)
            notificationManager?.ShowMessage("入群请求发送成功", NotificationType.Success, TimeSpan.FromSeconds(1.5));
        else
            notificationManager?.ShowMessage("入群请求发送失败", NotificationType.Error, TimeSpan.FromSeconds(1.5));

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
        GroupDto = parameters.GetValue<GroupDto>("GroupDto");
        Message = "我是" + _userManager.User!.Name;
        Group = "默认分组";
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}