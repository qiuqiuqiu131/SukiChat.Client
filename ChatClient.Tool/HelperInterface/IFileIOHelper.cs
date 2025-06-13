using ChatClient.Tool.Data.File;

namespace ChatClient.Tool.HelperInterface;

public interface IFileIOHelper
{
    Task<bool> UploadFileAsync(string targetPath, string fileName, Stream stream);

    Task<bool> UploadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress);

    Task<Stream?> GetFileAsync(string targetPath, string fileName, IProgress<double>? fileProgress = null);

    Task<bool> DownloadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress);
}