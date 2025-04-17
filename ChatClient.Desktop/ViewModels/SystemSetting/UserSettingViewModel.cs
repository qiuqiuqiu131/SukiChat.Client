using ChatClient.Tool.ManagerInterface;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class UserSettingViewModel : BindableBase
{
    private readonly IUserSetting _userSetting;

    public bool UseTurnServer
    {
        get => _userSetting.UseTurnServer;
        set
        {
            _userSetting.UseTurnServer = value;
            RaisePropertyChanged();
        }
    }

    public bool DoubleClickOpenExtendChatView
    {
        get => _userSetting.DoubleClickOpenExtendChatView;
        set
        {
            _userSetting.DoubleClickOpenExtendChatView = value;
            RaisePropertyChanged();
        }
    }


    public UserSettingViewModel(IUserSetting userSetting)
    {
        _userSetting = userSetting;
    }
}