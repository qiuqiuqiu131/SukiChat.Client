using System;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;

public class FriendRequestViewModel : ViewModelBase
{
    public AvaloniaList<FriendReceiveDto> FriendReceivedDtos { get; init; }
    public AvaloniaList<FriendRequestDto> FriendRequestDtos { get; init; }
    public AvaloniaList<FriendDeleteDto> FriendDeleteDtos { get; init; }

    public bool IsRequestEmpty =>
        FriendReceivedDtos.Count == 0 && FriendRequestDtos.Count == 0 && FriendDeleteDtos.Count == 0;

    public DelegateCommand<FriendReceiveDto> AcceptCommand { get; init; }
    public DelegateCommand<FriendReceiveDto> RejectCommand { get; init; }
    public DelegateCommand ClearAllCommand { get; init; }

    private bool isOperate = false;

    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IEventAggregator _eventAggregator;

    public FriendRequestViewModel(IContainerProvider containerProvider,
        ISukiDialogManager sukiDialogManager,
        IEventAggregator eventAggregator,
        IUserManager _userManager)
    {
        _containerProvider = containerProvider;
        _sukiDialogManager = sukiDialogManager;
        _eventAggregator = eventAggregator;

        FriendReceivedDtos = _userManager.FriendReceives!;
        FriendRequestDtos = _userManager.FriendRequests!;
        FriendDeleteDtos = _userManager.FriendDeletes!;
        FriendReceivedDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));
        FriendRequestDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));
        FriendDeleteDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));

        AcceptCommand = new DelegateCommand<FriendReceiveDto>(AcceptRequest);
        RejectCommand = new DelegateCommand<FriendReceiveDto>(RejectRequest);
        ClearAllCommand = new DelegateCommand(ClearAll);
    }

    private void ClearAll()
    {
        async void ClearAllCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;

            // 清空消息
            FriendDeleteDtos.Clear();
            FriendReceivedDtos.Clear();
            FriendRequestDtos.Clear();

            var userManager = _containerProvider.Resolve<IUserManager>();
            var userDto = userManager.User;
            userDto!.LastDeleteFriendMessageTime = DateTime.Now;
            await userManager.SaveUser();

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "好友消息已清空",
                Type = NotificationType.Information
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定清空所有消息吗？", ClearAllCallback))
            .TryShow();
    }

    private async void RejectRequest(FriendReceiveDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, false);
        if (state)
        {
            obj.IsAccept = false;
            obj.IsSolved = true;
            obj.SolveTime = DateTime.Now;
        }
        else
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "操作失败",
                Type = NotificationType.Error
            });

        isOperate = false;
    }

    private async void AcceptRequest(FriendReceiveDto obj)
    {
        if (isOperate) return;

        isOperate = true;
        var _friendService = _containerProvider.Resolve<IFriendService>();
        var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, true, "", "默认分组");
        if (state)
        {
            obj.IsAccept = true;
            obj.IsSolved = true;
            obj.SolveTime = DateTime.Now;
        }
        else
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "操作失败",
                Type = NotificationType.Error
            });

        isOperate = false;
    }
}