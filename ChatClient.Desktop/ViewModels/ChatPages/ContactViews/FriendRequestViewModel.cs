using System;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Ioc;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class FriendRequestViewModel : ViewModelBase
{
    public AvaloniaList<FriendReceiveDto> FriendReceivedDtos { get; init; }
    public AvaloniaList<FriendRequestDto> FriendRequestDtos { get; init; }

    public bool IsRequestEmpty => FriendReceivedDtos.Count == 0 && FriendRequestDtos.Count == 0;

    public DelegateCommand<FriendReceiveDto> AcceptCommand { get; init; }
    public DelegateCommand<FriendReceiveDto> RejectCommand { get; init; }

    private bool isOperate = false;

    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _dialogManager;
    private readonly ISukiToastManager _toastManager;

    public FriendRequestViewModel(IContainerProvider containerProvider,
        ISukiDialogManager dialogManager,
        ISukiToastManager toastManager,
        IUserManager _userManager)
    {
        _containerProvider = containerProvider;
        _dialogManager = dialogManager;
        _toastManager = toastManager;

        FriendReceivedDtos = _userManager.FriendReceives!;
        FriendRequestDtos = _userManager.FriendRequests!;
        FriendReceivedDtos.CollectionChanged += (sender, args) => { RaisePropertyChanged(nameof(IsRequestEmpty)); };
        FriendRequestDtos.CollectionChanged += (sender, args) => { RaisePropertyChanged(nameof(IsRequestEmpty)); };

        AcceptCommand = new DelegateCommand<FriendReceiveDto>(AcceptRequest);
        RejectCommand = new DelegateCommand<FriendReceiveDto>(RejectRequest);
    }

    private async void RejectRequest(FriendReceiveDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, false, "默认分组");
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

    private async void AcceptRequest(FriendReceiveDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, true, "默认分组");
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
}