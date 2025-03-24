using System.Collections.Generic;
using System.Linq;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.SearchUserGroup;

public class AddGroupRequestViewModel : BindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly ISukiToastManager _toastManager;

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

    public DelegateCommand SendGroupRequestCommand { get; }
    public DelegateCommand CancleCommand { get; }

    public AddGroupRequestViewModel(IContainerProvider containerProvider,
        IUserManager userManager,
        ISukiToastManager toastManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        _toastManager = toastManager;

        SendGroupRequestCommand = new DelegateCommand(SendGroupRequest);
        CancleCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    private async void SendGroupRequest()
    {
        var _groupService = _containerProvider.Resolve<IGroupService>();
        var (state, message) =
            await _groupService.JoinGroupRequest(_userManager.User!.Id, _groupDto.Id,
                Message ?? "");
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