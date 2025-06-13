namespace ChatClient.Tool.HelperInterface;

public enum FileTarget
{
    User,
    Group
}

public interface IFileOperateHelper
{
    Task<Stream> GetGroupFile(string path, string fileName);
    Task<Stream> GetFile(string id, string path, string fileName, FileTarget fileTarget);
    Task<bool> UploadFile(string id, string path, string fileName, Stream stream, FileTarget fileTarget);
    Task<bool> SaveAsFile(string id, string path, string fileName, string filePath, FileTarget fileTarget);
}