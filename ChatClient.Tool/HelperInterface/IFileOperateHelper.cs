namespace ChatClient.Tool.HelperInterface;

public enum FileTarget
{
    User,
    Group
}

public interface IFileOperateHelper
{
    Task<byte[]?> GetGroupFile(string path, string fileName);
    Task<byte[]?> GetFile(string id, string path, string fileName, FileTarget fileTarget);
    Task<bool> UploadFile(string id, string path, string fileName, byte[] bytes, FileTarget fileTarget);
}