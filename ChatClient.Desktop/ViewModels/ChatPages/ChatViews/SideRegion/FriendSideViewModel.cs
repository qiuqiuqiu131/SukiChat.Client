using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.SideRegion;

public class FriendSideViewModel : BindableBase, INavigationAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _sukiDialogManager;

    private IRegionNavigationService _regionManager;

    private FriendRelationDto _selectedFriend;

    public FriendRelationDto SelectedFriend
    {
        get => _selectedFriend;
        set => SetProperty(ref _selectedFriend, value);
    }

    public DelegateCommand DeleteCommand { get; }

    public FriendSideViewModel(IContainerProvider containerProvider, IUserManager userManager,
        IEventAggregator eventAggregator,
        ISukiDialogManager sukiDialogManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        _eventAggregator = eventAggregator;
        _sukiDialogManager = sukiDialogManager;
        DeleteCommand = new DelegateCommand(DeleteFriend);
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    private async void DeleteFriend()
    {
        if (SelectedFriend == null) return;

        async void DeleteFriendCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;
            var friendService = _containerProvider.Resolve<IFriendService>();
            await friendService.DeleteFriend(_userManager.User!.Id, SelectedFriend.Id);

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = $"您已删除好友 {SelectedFriend.UserDto?.Name ?? string.Empty}",
                Type = NotificationType.Success
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定删除好友吗？", DeleteFriendCallback))
            .TryShow();
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedFriend = navigationContext.Parameters.GetValue<FriendRelationDto>("SelectedFriend");
        _regionManager = navigationContext.NavigationService;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    #endregion
}