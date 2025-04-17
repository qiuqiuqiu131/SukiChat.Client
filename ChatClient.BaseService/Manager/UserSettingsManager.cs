using System.Text.Json;
using System.Text.Json.Nodes;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

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
            var json = JsonSerializer.Serialize(this);
            System.IO.File.WriteAllText(settingPath, json);
        }
        else
        {
            var json = System.IO.File.ReadAllText(settingPath);
            var settings = JsonNode.Parse(json);
            if (settings != null)
            {
                UseTurnServer = settings["UseTurnServer"]?.GetValue<bool>() ?? true;
                DoubleClickOpenExtendChatView = settings["DoubleClickOpenExtendChatView"]?.GetValue<bool>() ?? false;
            }
        }
    }

    private void SaveSettings()
    {
        var fileInfo = new FileInfo(settingPath);
        if (!fileInfo.Exists)
            fileInfo.Create().Dispose();

        var json = JsonSerializer.Serialize(this);
        System.IO.File.WriteAllText(settingPath, json);
    }
}