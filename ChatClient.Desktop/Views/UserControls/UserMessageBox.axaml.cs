using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService.Manager;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using Prism.Ioc;

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

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish();
    }
}