using System.Net;
using System.Text;
using System.Text.Json;
using ChatClient.Android.Shared.Services;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Tool.Config;
using SocketClient.Client;
#if ANDROID
using AndApplication = Android.App.Application;
#endif

namespace ChatClient.Android.Shared.ViewModels;

[RegionMemberLifetime(KeepAlive = false)]
public class NetSettingViewModel : SidePageViewModelBase
{
    private readonly AppSettings _appSettings;
    private readonly ISocketClient _client;
    private readonly string _appSettingsPath;

    private string _origionIpAddress;

    private IPAddress? _ipAddress;

    public IPAddress? IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    private int _origionPort;

    private int? _port;

    public int? Port
    {
        get => _port;
        set
        {
            if (value is < 0 or >= 65536)
                RaisePropertyChanged();
            else if (value == null)
                RaisePropertyChanged();
            else
                SetProperty(ref _port, value);
        }
    }

    public DelegateCommand ApplyCommand { get; }
    public DelegateCommand ReturnCommand { get; }

    public NetSettingViewModel(AppSettings appSettings, ISocketClient client,
        IContainerProvider containerProvider) : base(containerProvider)
    {
        _appSettings = appSettings;
        _client = client;
#if ANDROID
        _appSettingsPath = Path.Combine(AndApplication.Context.FilesDir.AbsolutePath, "appsettings.json");
#else
        _appSettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
#endif

        _origionIpAddress = appSettings.Address.Ip;
        var canParse = IPAddress.TryParse(_origionIpAddress, out _ipAddress);
        if (!canParse)
            _ipAddress = IPAddress.Parse("127.0.0.1");

        _origionPort = appSettings.Address.MainPort;
        _port = _origionPort;

        ApplyCommand = new DelegateCommand(Apply);
        ReturnCommand = new DelegateCommand(ReturnBack);
    }

    private void Apply()
    {
        var ipAddress = IPAddress?.ToString() ?? "127.0.0.1";
        var port = Port ?? 0;
        if (_origionIpAddress != ipAddress || _origionPort != port)
        {
            UpdateAppSetting(ipAddress, port);
            _client.ChangeAddress();
        }

        ReturnBack();
    }

    private void UpdateAppSetting(string ip, int port)
    {
        _appSettings.Address.Ip = ip;
        _appSettings.Address.MainPort = port;

        // 写会配置文件
        var settings = JsonSerializer.Serialize(_appSettings, AppSettingsContext.Default.AppSettings);
        System.IO.File.WriteAllText(_appSettingsPath, settings, Encoding.UTF8);
    }
}