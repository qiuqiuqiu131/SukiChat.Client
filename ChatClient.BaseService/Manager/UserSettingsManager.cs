using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

[JsonSerializable(typeof(UserSettings))]
public partial class UserSettingsContext : JsonSerializerContext
{
}

public class UserSettings
{
    public bool UseTurnServer { get; set; }
    public bool DoubleClickOpenExtendChatView { get; set; }
}

public class UserSettingsManager : IUserSetting
{
    private bool userTurnServer = true;

    public bool UseTurnServer
    {
        get => userTurnServer;
        set
        {
            userTurnServer = value;
            SaveSettings();
        }
    }

    private bool doubleClickOpenExtendChatView;

    public bool DoubleClickOpenExtendChatView
    {
        get => doubleClickOpenExtendChatView;
        set
        {
            doubleClickOpenExtendChatView = value;
            SaveSettings();
        }
    }

    private readonly string settingPath;

    public UserSettingsManager(IAppDataManager appDataManager)
    {
        settingPath = appDataManager.GetFilePath("UserSettings.json");
        var fileInfo = new FileInfo(settingPath);

        if (!fileInfo.Exists)
        {
            fileInfo.Create().Dispose();
            var setting = new UserSettings
            {
                UseTurnServer = UseTurnServer,
                DoubleClickOpenExtendChatView = DoubleClickOpenExtendChatView
            };
            var json = JsonSerializer.Serialize(setting, UserSettingsContext.Default.UserSettings);
            System.IO.File.WriteAllText(settingPath, json);
        }
        else
        {
            var json = System.IO.File.ReadAllText(settingPath);
            var setting = JsonSerializer.Deserialize(json, UserSettingsContext.Default.UserSettings);
            if (setting != null)
            {
                UseTurnServer = setting.UseTurnServer;
                DoubleClickOpenExtendChatView = setting.DoubleClickOpenExtendChatView;
            }
        }
    }

    private void SaveSettings()
    {
        var fileInfo = new FileInfo(settingPath);
        if (!fileInfo.Exists)
            fileInfo.Create().Dispose();

        var setting = new UserSettings
        {
            UseTurnServer = UseTurnServer,
            DoubleClickOpenExtendChatView = DoubleClickOpenExtendChatView
        };
        var json = JsonSerializer.Serialize(setting, UserSettingsContext.Default.UserSettings);
        System.IO.File.WriteAllText(settingPath, json);
    }
}