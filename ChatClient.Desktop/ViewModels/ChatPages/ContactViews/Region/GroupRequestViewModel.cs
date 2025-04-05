using System;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;

public class GroupRequestViewModel : ViewModelBase, IRegionMemberLifetime
{
    public AvaloniaList<GroupReceivedDto> GroupReceivedDtos { get; init; }
    public AvaloniaList<GroupRequestDto> GroupRequestDtos { get; init; }
    public AvaloniaList<GroupDeleteDto> GroupDeleteDtos { get; init; }

    public bool IsRequestEmpty =>
        GroupReceivedDtos.Count == 0 && GroupRequestDtos.Count == 0 && GroupDeleteDtos.Count == 0;

    public DelegateCommand<GroupReceivedDto> AcceptCommand { get; init; }
    public DelegateCommand<GroupReceivedDto> RejectCommand { get; init; }
    public DelegateCommand ClearAllCommand { get; init; }

    private bool isOperate = false;

    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;

    public GroupRequestViewModel(IContainerProvider containerProvider,
        ISukiDialogManager sukiDialogManager,
        IEventAggregator eventAggregator,
        IUserManager _userManager)
    {
        _containerProvider = containerProvider;
        _sukiDialogManager = sukiDialogManager;
        _eventAggregator = eventAggregator;
        this._userManager = _userManager;

        GroupReceivedDtos = _userManager.GroupReceiveds!;
        GroupRequestDtos = _userManager.GroupRequests!;
        GroupDeleteDtos = _userManager.GroupDeletes!;
        GroupReceivedDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));
        GroupRequestDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));
        GroupDeleteDtos.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(IsRequestEmpty));

        AcceptCommand = new DelegateCommand<GroupReceivedDto>(AcceptRequest);
        RejectCommand = new DelegateCommand<GroupReceivedDto>(RejectRequest);
        ClearAllCommand = new DelegateCommand(ClearAll);
    }

    private void ClearAll()
    {
        async void ClearAllCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;

            GroupReceivedDtos.Clear();
            GroupRequestDtos.Clear();
            GroupDeleteDtos.Clear();

            var userManager = _containerProvider.Resolve<IUserManager>();
            var userDto = userManager.User;
            userDto!.LastDeleteGroupMessageTime = DateTime.Now;
            await userManager.SaveUser();

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "群聊消息已清空",
                Type = NotificationType.Information
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定清空所有消息吗？", ClearAllCallback))
            .TryShow();
    }

    private async void RejectRequest(GroupReceivedDto obj)
    {
        if (isOperate) return;

        async void RejectRequestCallback(IDialogResult result)
        {
            isOperate = false;
            if (result.Result != ButtonResult.OK) return;

            var _groupService = _containerProvider.Resolve<IGroupService>();
            var (state, message) = await _groupService.JoinGroupResponse(_userManager.User.Id, obj.RequestId, false);
            if (!state)
            {
                _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "操作失败",
                    Type = NotificationType.Error
                });
            }
        }

        isOperate = true;
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定拒绝好友请求吗？", RejectRequestCallback))
            .TryShow();
    }

    private void AcceptRequest(GroupReceivedDto obj)
    {
        if (isOperate) return;

        async void AcceptRequestCallback(IDialogResult result)
        {
            isOperate = false;
            if (result.Result != ButtonResult.OK) return;

            var _groupService = _containerProvider.Resolve<IGroupService>();
            var (state, message) = await _groupService.JoinGroupResponse(_userManager.User.Id, obj.RequestId, true);
            if (!state)
            {
                _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "操作失败",
                    Type = NotificationType.Error
                });
            }
        }

        isOperate = true;
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定拒绝好友请求吗？", AcceptRequestCallback))
            .TryShow();
    }

    public bool KeepAlive => false;
}