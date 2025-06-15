using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.Login;

[RegionMemberLifetime(KeepAlive = false)]
public class LoginSettingViewModel : ViewModelBase
{
    private readonly ILoginService _loginService;

    private IRegionNavigationService? _navigationService;

    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    private ObservableCollection<LoginUserItem>? userList;

    public ObservableCollection<LoginUserItem>? UserList
    {
        get => userList;
        set => SetProperty(ref userList, value);
    }

    public AsyncDelegateCommand<LoginUserItem> RemoveLoginCommand { get; }
    public DelegateCommand ReturnCommand { get; }

    public LoginSettingViewModel(ILoginService loginService, IConnection connection)
    {
        IsConnected = connection.IsConnected;
        _loginService = loginService;
        RemoveLoginCommand = new AsyncDelegateCommand<LoginUserItem>(RemoveLogin);
        ReturnCommand = new DelegateCommand(() => { _navigationService.RequestNavigate(nameof(LoginView)); });
    }

    private async Task RemoveLogin(LoginUserItem loginData)
    {
        await _loginService.RemoveLoginUser(loginData.ID);
        userList?.Remove(loginData);
    }

    #region IRegionAware

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters.TryGetValue("UserList", out ObservableCollection<LoginUserItem>? list))
            UserList = list;
        _navigationService = navigationContext.NavigationService;
    }

    #endregion
}