using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Helper;

public interface IFileOperateHelper
{
    Task<byte[]?> GetFileForUser(string path, string fileName);
    Task<bool> UploadFileForUser(string path, string fileName, byte[] bytes);
}

/// <summary>
/// 用于文件的处理，通过本地保存和Web请求，获取和保存文件内容
/// </summary>
internal class FileOperateHelper : IFileOperateHelper
{
    private readonly IFileIOHelper _fileIOHelper;
    private readonly IAppDataManager _appDataManager;

    public FileOperateHelper(IFileIOHelper webApiHelper, IAppDataManager appDataManager)
    {
        _fileIOHelper = webApiHelper;
        _appDataManager = appDataManager;
    }

    /// <summary>
    /// 用于用户信息的文件获取
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetFileForUser(string path, string fileName)
    {
        var fileInfo = _appDataManager.GetFileInfo(Path.Combine(path, fileName));
        if (fileInfo.Exists)
            return await System.IO.File.ReadAllBytesAsync(fileInfo.FullName);
        else
        {
            byte[]? file = await _fileIOHelper.GetFileAsync(Path.Combine("Users", path), fileName);
            if (file != null)
            {
                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                await System.IO.File.WriteAllBytesAsync(fileInfo.FullName, file);
                return file;
            }
            else
            {
                // TODO : 未找到该文件
                return null;
            }
        }
    }


    /// <summary>
    /// 用于用户信息的文件保存
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public async Task<bool> UploadFileForUser(string path, string fileName, byte[] bytes)
    {
        var result = await _fileIOHelper.UploadFileAsync(Path.Combine("Users", path), fileName, bytes);

        // 如果上传失败，返回false
        if (!result) return false;

        // 上传成功，保存到本地 
        var fileInfo = _appDataManager.GetFileInfo(Path.Combine(path, fileName));
        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            Directory.CreateDirectory(fileInfo.DirectoryName!);
        await System.IO.File.WriteAllBytesAsync(fileInfo.FullName, bytes);
        return true;
    }
}