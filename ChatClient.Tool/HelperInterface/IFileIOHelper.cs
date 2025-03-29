using ChatClient.Tool.Data.File;

namespace ChatClient.Tool.HelperInterface;

public interface IFileIOHelper
{
    Task<bool> UploadFileAsync(string targetPath, string fileName, byte[] file);

    Task<bool> UploadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress);

    Task<byte[]?> GetFileAsync(string targetPath, string fileName, IProgress<double>? fileProgress);

    Task<bool> DownloadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress);


    Task<byte[]?> GetCompressedFileAsync(string targetPath, string fileName);
}