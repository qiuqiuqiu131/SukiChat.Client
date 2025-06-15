using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatClient.Client;
using Microsoft.Extensions.Configuration;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.Login;

public class NetSettingViewModel : BindableBase, IDialogAware
{
    private readonly IConfigurationRoot _configuration;
    private readonly ISocketClient _client;
    private readonly string _appSettingsPath;

    private IPAddress? _ipAddress;

    public IPAddress? IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public DelegateCommand ApplyCommand { get; }

    public NetSettingViewModel(IConfigurationRoot configuration, ISocketClient client)
    {
        _configuration = configuration;
        _client = client;
        _appSettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

        var canParse = IPAddress.TryParse(configuration["Address:IP"], out _ipAddress);
        if (!canParse)
            _ipAddress = IPAddress.Parse("127.0.0.1");

        ApplyCommand = new DelegateCommand(Apply);
    }

    private void Apply()
    {
        UpdateAppSetting("Address:IP", _ipAddress?.ToString() ?? "127.0.0.1");
        _client.ChangeAddress();
        RequestClose.Invoke();
    }

    private void UpdateAppSetting(string sectionPath, string value)
    {
        // 1. 读取当前 appsettings.json 内容
        var json = System.IO.File.ReadAllText(_appSettingsPath, Encoding.UTF8);

        // 2. 将JSON解析为JsonNode
        var jsonObj = JsonNode.Parse(json);
        if (jsonObj == null)
        {
            throw new InvalidOperationException("Failed to parse appsettings.json");
        }

        // 3. 分割section路径（支持嵌套配置）
        var sections = sectionPath.Split(':');
        var currentNode = jsonObj;

        // 4. 导航到目标节点（除了最后一个部分）
        for (int i = 0; i < sections.Length - 1; i++)
        {
            var section = sections[i];
            if (currentNode[section] == null)
            {
                currentNode[section] = new JsonObject();
            }

            currentNode = currentNode[section];
        }

        // 5. 设置最终值
        var finalSection = sections[^1];
        currentNode[finalSection] = value;

        // 6. 序列化回JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var output = jsonObj.ToJsonString(options);

        // 7. 写回文件
        System.IO.File.WriteAllText(_appSettingsPath, output, Encoding.UTF8);

        // 8. 重新加载配置
        _configuration.Reload();
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