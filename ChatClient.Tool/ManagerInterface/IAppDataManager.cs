namespace ChatClient.Tool.ManagerInterface;

public interface IAppDataManager
{
    string GetBaseFloaderPath();
    string GetFilePath(string path);
    FileInfo GetFileInfo(string path);
}