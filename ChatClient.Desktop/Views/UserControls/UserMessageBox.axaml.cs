using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.Views.UserControls;

public partial class UserMessageBox : UserControl
{
    private readonly IUserManager _userManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserDtoManager _userDtoManager;

    public UserMessageBox()
    {
        InitializeComponent();
        _userManager = App.Current.Container.Resolve<IUserManager>();
        _eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        _userDtoManager = App.Current.Container.Resolve<IUserDtoManager>();
    }

    private async void SendMessageToView(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UserDto { IsUser: false, IsFriend: true } userDto)
        {
            var relation = await _userDtoManager.GetFriendRelationDto(_userManager.User!.Id, userDto.Id);
            if (relation != null)
            {
                _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
                _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(relation);
            }
        }
    }

    private void AddFriend(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UserDto { IsUser: false, IsFriend: false } userDto)
        {
            var dialogService = App.Current.Container.Resolve<IDialogService>();
            dialogService.Show(nameof(AddFriendRequestView), new DialogParameters { { "UserDto", userDto } }, _ => { });
        }
    }

    private void EditUserData(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UserDto { IsUser: true } userDto)
        {
            var dialogService = App.Current.Container.Resolve<ISukiDialogManager>();
            dialogService.CreateDialog()
                .WithViewModel(d => new EditUserDataViewModel(d, userDto))
                .TryShow();
        }
    }

    private void ShareFriend(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UserDto { IsUser: false, IsFriend: true } userDto)
        {
            var dialogService = App.Current.Container.Resolve<ISukiDialogManager>();
            dialogService.CreateDialog()
                .WithViewModel(d => new ShareViewModel(d, new DialogParameters
                {
                    { "ShareMess", new CardMessDto { IsUser = true, Id = userDto.Id } },
                    { "ShowMess", false }
                }, null))
                .TryShow();
        }
    }
}