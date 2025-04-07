using Avalonia.Media.Imaging;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.Tool.ManagerInterface;

public interface IImageManager
{
    Task<Bitmap?> GetGroupFile(string path, string fileName);
    Task<Bitmap?> GetFile(string id, string path, string fileName, FileTarget fileTarget);
    Task<Bitmap?> GetChatFile(string id, string path, string fileName, FileTarget fileTarget);
    Task<Bitmap> GetStaticFile(string path);

    int CleanupUnusedChatImages(int maxAgeMinutes = 10);

    void ClearCache();
    bool RemoveFromCache(string id, string path, string fileName, FileTarget? fileTarget = null);
}