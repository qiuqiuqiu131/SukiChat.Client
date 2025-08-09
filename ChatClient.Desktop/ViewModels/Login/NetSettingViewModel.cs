using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using ChatClient.Avalonia.Common;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Config;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SocketClient.Client;

namespace ChatClient.Desktop.ViewModels.Login;

public class NetSettingViewModel : ViewModelBase
{
    private readonly AppSettings _appSettings;
    private readonly ISocketClient _client;
    private readonly string _appSettingsPath;

    private bool isApplyed = false;

    private IRegionNavigationService? _navigationService;

    private IPAddress? _ipAddress;

    public IPAddress? IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    public DelegateCommand ApplyCommand { get; }
    public DelegateCommand ReturnCommand { get; }

    public NetSettingViewModel(AppSettings appSettings, ISocketClient client, IConnection connection)
    {
        _appSettings = appSettings;
        _client = client;
        _appSettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

        IsConnected = connection.IsConnected;

        var canParse = IPAddress.TryParse(appSettings.Address.Ip, out _ipAddress);
        if (!canParse)
            _ipAddress = IPAddress.Parse("127.0.0.1");

        ApplyCommand = new DelegateCommand(Apply);
        ReturnCommand = new DelegateCommand(() => { _navigationService.RequestNavigate(nameof(LoginView)); });
    }

    private void Apply()
    {
        if (isApplyed) return;

        isApplyed = true;
        UpdateAppSetting(IPAddress?.ToString() ?? "127.0.0.1");
        _client.ChangeAddress();
        _navigationService.RequestNavigate(nameof(LoginView));
    }

    private void UpdateAppSetting(string ip)
    {
        _appSettings.Address.Ip = ip;

        // 写会配置文件
        var settings = JsonSerializer.Serialize(_appSettings, AppSettingsContext.Default.AppSettings);
        System.IO.File.WriteAllText(_appSettingsPath, settings, Encoding.UTF8);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        _navigationService = navigationContext.NavigationService;
    }
}