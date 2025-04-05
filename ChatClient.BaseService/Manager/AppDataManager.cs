using ChatClient.Tool.ManagerInterface;
using Microsoft.Extensions.Configuration;

namespace ChatClient.BaseService.Manager;

internal class AppDataManager : IAppDataManager
{
    private readonly string _appDataFolder;

    public AppDataManager(IConfigurationRoot configurationRoot)
    {
        var floaderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _appDataFolder = Path.Combine(floaderPath, "QiuQiuQiu", configurationRoot["BaseFolder"] ?? "ChatApp");

        // Create the folder if it doesn't exist
        if (!Directory.Exists(_appDataFolder))
            Directory.CreateDirectory(_appDataFolder);
    }

    public string GetBaseFloaderPath() => _appDataFolder;

    public string GetFilePath(string path)
    {
        return Path.Combine(_appDataFolder, path);
    }


    public FileInfo GetFileInfo(string path)
    {
        var filePath = Path.Combine(_appDataFolder, path);
        return new FileInfo(filePath);
    }
}