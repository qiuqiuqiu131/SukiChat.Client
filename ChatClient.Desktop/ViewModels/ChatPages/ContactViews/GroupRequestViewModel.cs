using System;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Ioc;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class GroupRequestViewModel : ViewModelBase
{
    public AvaloniaList<GroupReceivedDto> GroupReceivedDtos { get; init; }
    public AvaloniaList<GroupRequestDto> GroupRequestDtos { get; init; }

    public bool IsRequestEmpty => GroupReceivedDtos.Count == 0 && GroupRequestDtos.Count == 0;

    public DelegateCommand<GroupReceivedDto> AcceptCommand { get; init; }
    public DelegateCommand<GroupReceivedDto> RejectCommand { get; init; }
    private bool isOperate = false;

    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _dialogManager;
    private readonly ISukiToastManager _toastManager;
    private readonly IUserManager _userManager;

    public GroupRequestViewModel(IContainerProvider containerProvider,
        ISukiDialogManager dialogManager,
        ISukiToastManager toastManager,
        IUserManager _userManager)
    {
        _containerProvider = containerProvider;
        _dialogManager = dialogManager;
        _toastManager = toastManager;
        this._userManager = _userManager;

        GroupReceivedDtos = _userManager.GroupReceiveds!;
        GroupRequestDtos = _userManager.GroupRequests!;
        GroupReceivedDtos.CollectionChanged += (sender, args) => { RaisePropertyChanged(nameof(IsRequestEmpty)); };
        GroupReceivedDtos.CollectionChanged += (sender, args) => { RaisePropertyChanged(nameof(IsRequestEmpty)); };

        AcceptCommand = new DelegateCommand<GroupReceivedDto>(AcceptRequest);
        RejectCommand = new DelegateCommand<GroupReceivedDto>(RejectRequest);
    }

    private async void RejectRequest(GroupReceivedDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _groupService = _containerProvider.Resolve<IGroupService>();
        var (state, message) = await _groupService.JoinGroupResponse(_userManager.User.Id, obj.RequestId, false);
        if (state)
        {
            obj.IsAccept = true;
            obj.IsSolved = true;
            obj.SolveTime = DateTime.Now;
        }
        else
            _toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("操作失败")
                .WithContent(message)
                .Queue();

        isOperate = false;
    }

    private async void AcceptRequest(GroupReceivedDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _groupService = _containerProvider.Resolve<IGroupService>();
        var (state, message) = await _groupService.JoinGroupResponse(_userManager.User.Id, obj.RequestId, true);
        if (state)
        {
            obj.IsAccept = true;
            obj.IsSolved = true;
            obj.SolveTime = DateTime.Now;
            obj.AcceptByUserId = _userManager.User.Id;
        }
        else
            _toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("操作失败")
                .WithContent(message)
                .Queue();

        isOperate = false;
    }
}