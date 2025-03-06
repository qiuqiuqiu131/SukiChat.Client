using System.Text.Json;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

internal class LoginDataManager : ILoginData
{
    public LoginData LoginData { get; private set; }

    public LoginDataManager(IAppDataManager appDataManager)
    {
        var fileInfo = appDataManager.GetFileInfo("LoginData.json");

        if (!fileInfo.Exists)
            LoginData = new LoginData();
        else
        {
            var data = System.IO.File.ReadAllText(fileInfo.FullName);
            LoginData = JsonSerializer.Deserialize<LoginData>(data) ?? new LoginData();
        }

        LoginData.LoginDataChanged += delegate
        {
            System.IO.File.WriteAllText(fileInfo.FullName, JsonSerializer.Serialize(LoginData));
        };
    }
}