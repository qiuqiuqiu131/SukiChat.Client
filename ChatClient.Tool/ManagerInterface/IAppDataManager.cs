namespace ChatClient.Tool.ManagerInterface;

public interface IAppDataManager
{
    string GetBaseFolderPath();
    string GetFilePath(string path);
    FileInfo GetFileInfo(string path);
}