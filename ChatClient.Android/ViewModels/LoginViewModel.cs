using Android.App;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Mvvm;

namespace ChatClient.Android.ViewModels;

public class LoginViewModel : BindableBase
{
    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public LoginViewModel(IConnection connection)
    {
        IsConnected = connection.IsConnected;
    }
}