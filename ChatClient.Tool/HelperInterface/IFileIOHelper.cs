using ChatClient.Tool.Data.File;

namespace ChatClient.Tool.HelperInterface;

public interface IFileIOHelper
{
    Task<bool> UploadFileAsync(string targetPath, string fileName, byte[] file);

    Task<bool> UploadLargeFileAsync(string targetPath, string fileName, string filePath,
        FileProcessDto? fileProcessDto = null);

    Task<byte[]?> GetFileAsync(string targetPath, string fileName);

    Task<bool> DownloadLargeFileAsync(string targetPath, string fileName, string filePath,
        FileProcessDto? fileProcessDto = null);

    Task<byte[]?> GetCompressedFileAsync(string targetPath, string fileName);
}