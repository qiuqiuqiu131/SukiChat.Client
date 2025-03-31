using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.Views.UserControls;

public partial class GroupMessageBox : UserControl
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUserManager _userManager;

    public GroupMessageBox()
    {
        InitializeComponent();
        _eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        _userDtoManager = App.Current.Container.Resolve<IUserDtoManager>();
        _userManager = App.Current.Container.Resolve<IUserManager>();
    }

    private async void SendMessageToView(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GroupDto { IsEntered: true } groupDto)
        {
            var relation = await _userDtoManager.GetGroupRelationDto(_userManager.User!.Id, groupDto.Id);
            if (relation != null)
                _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(relation);
        }
    }

    private void AddGroup(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GroupDto { IsEntered: false } groupDto)
        {
            var dialogService = App.Current.Container.Resolve<IDialogService>();
            dialogService.Show(nameof(AddGroupRequestView), new DialogParameters { { "GroupDto", groupDto } },
                _ => { });
        }
    }

    private void ShareGroup(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GroupDto { IsEntered: true } groupDto)
        {
            var dialogService = App.Current.Container.Resolve<ISukiDialogManager>();
            dialogService.CreateDialog()
                .WithViewModel(d => new ShareViewModel(d, new DialogParameters
                {
                    { "ShareMess", new CardMessDto { IsUser = false, Id = groupDto.Id } }
                }, null))
                .TryShow();
        }
    }
}