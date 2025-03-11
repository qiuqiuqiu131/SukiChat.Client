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

    public bool IsRequestEmpty => FriendReceivedDtos.Count == 0;

    public DelegateCommand<FriendReceiveDto> ResponseFriendRequestCommand { get; init; }

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
        FriendReceivedDtos.CollectionChanged += (sender, args) => { RaisePropertyChanged(nameof(IsRequestEmpty)); };

        ResponseFriendRequestCommand = new DelegateCommand<FriendReceiveDto>(ResponseFriendRequest);
    }

    // 响应好友请求
    private void ResponseFriendRequest(FriendReceiveDto obj)
    {
        _dialogManager.CreateDialog()
            .WithTitle("好友请求")
            .WithContent($"是否同意{obj.UserFromId}的好友请求")
            .WithActionButton("Yes", async d =>
            {
                var _friendService = _containerProvider.Resolve<IFriendService>();
                var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, true, "默认分组");
                if (state)
                    FriendReceivedDtos.Remove(obj);
                else
                    _toastManager.CreateToast()
                        .OfType(NotificationType.Error)
                        .WithTitle("操作失败")
                        .WithContent(message)
                        .Queue();
            }, true)
            .WithActionButton("No", async d =>
            {
                var _friendService = _containerProvider.Resolve<IFriendService>();
                var (state, message) = await _friendService.ResponseFriendRequest(obj.RequestId, false, "默认分组");
                if (state)
                    FriendReceivedDtos.Remove(obj);
                else
                    _toastManager.CreateToast()
                        .OfType(NotificationType.Error)
                        .WithTitle("操作失败")
                        .WithContent(message)
                        .Queue();
            }, true)
            .Dismiss().ByClickingBackground()
            .TryShow();
    }
}