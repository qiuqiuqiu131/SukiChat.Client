using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using SukiUI.Dialogs;
using SukiUI.Enums;

namespace ChatClient.Desktop.ViewModels.Login;

public class LoginWindowViewModel : ViewModelBase
{
    public ISukiDialogManager DialogManager { get; init; }

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private ThemeStyle _currentThemeStyle;

    public ThemeStyle CurrentThemeStyle
    {
        get => _currentThemeStyle;
        private set => SetProperty(ref _currentThemeStyle, value);
    }

    public LoginWindowViewModel(IThemeStyle themeStyle, IConnection connection, ISukiDialogManager dialogManager)
    {
        DialogManager = dialogManager;

        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;
    }
}