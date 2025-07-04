using System;
using System.IO;
using System.Runtime.InteropServices;
using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Desktop.Services;

internal class DesktopAppDataManager : IAppDataManager
{
    private readonly string _appDataFolder;

    public DesktopAppDataManager(AppSettings appSettings)
    {
        string baseFolder;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var cacheFolder = Environment.GetEnvironmentVariable("XDG_CACHE_HOME") ??
                              Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            baseFolder = Path.Combine(cacheFolder, "QiuQiuQiu");
        }
        else
        {
            baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QiuQiuQiu");
        }

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
}