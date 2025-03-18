using Avalonia.Media.Imaging;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.Tool.ManagerInterface;

public interface IImageManager
{
    Task<Bitmap?> GetGroupFile(string path, string fileName);
    Task<Bitmap?> GetFile(string id, string path, string fileName, FileTarget fileTarget);
    void ClearCache();
    bool RemoveFromCache(string id, string path, string fileName, FileTarget? fileTarget = null);
}