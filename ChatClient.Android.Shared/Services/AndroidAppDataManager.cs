#if ANDROID
using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;
using Application = Android.App.Application;

namespace ChatClient.Android.Shared.Services;

class AndroidAppDataManager : IAppDataManager
{
    private readonly string _appDataFolder;

    public AndroidAppDataManager(AppSettings appSettings)
    {
        string? baseFolder = Application.Context.CacheDir?.AbsolutePath;

        if (baseFolder == null)
            throw new InvalidOperationException("Cache directory is not available.");

        _appDataFolder = Path.Combine(baseFolder, appSettings.BaseFolder);

        if (!Directory.Exists(_appDataFolder))
            Directory.CreateDirectory(_appDataFolder);
    }

    public string GetBaseFolderPath() => _appDataFolder;

    public string GetFilePath(string path)
    {
        return Path.Combine(_appDataFolder, path);
    }

    public FileInfo GetFileInfo(string path)
    {
        var filePath = Path.Combine(_appDataFolder, path);
        return new FileInfo(filePath);
    }

    public string? GetConfigPath()
    {
        return Application.Context.FilesDir?.AbsolutePath;
    }
}
#endif