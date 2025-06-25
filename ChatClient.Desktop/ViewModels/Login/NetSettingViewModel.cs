using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using ChatClient.Tool.Config;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SocketClient.Client;

namespace ChatClient.Desktop.ViewModels.Login;

public class NetSettingViewModel : BindableBase, IDialogAware
{
    private readonly AppSettings _appSettings;
    private readonly ISocketClient _client;
    private readonly string _appSettingsPath;

    private IPAddress? _ipAddress;

    public IPAddress? IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public DelegateCommand ApplyCommand { get; }

    public NetSettingViewModel(AppSettings appSettings, ISocketClient client)
    {
        _appSettings = appSettings;
        _client = client;
        _appSettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

        var canParse = IPAddress.TryParse(appSettings.Address.Ip, out _ipAddress);
        if (!canParse)
            _ipAddress = IPAddress.Parse("127.0.0.1");

        ApplyCommand = new DelegateCommand(Apply);
    }

    private void Apply()
    {
        UpdateAppSetting(IPAddress?.ToString() ?? "127.0.0.1");
        _client.ChangeAddress();
        RequestClose.Invoke();
    }

    private void UpdateAppSetting(string ip)
    {
        _appSettings.Address.Ip = ip;

        // 写会配置文件
        var settings = JsonSerializer.Serialize(_appSettings, AppSettingsContext.Default.AppSettings);
        System.IO.File.WriteAllText(_appSettingsPath, settings, Encoding.UTF8);
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}