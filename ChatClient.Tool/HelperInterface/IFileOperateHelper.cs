namespace ChatClient.Tool.HelperInterface;

public enum FileTarget
{
    User,
    Group
}

public interface IFileOperateHelper
{
    Task<byte[]?> GetFile(string userId, string path, string fileName, FileTarget fileTarget);
    Task<bool> UploadFile(string userId, string path, string fileName, byte[] bytes, FileTarget fileTarget);
}