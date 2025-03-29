using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Helper;

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

    public async Task<byte[]?> GetGroupFile(string path, string fileName)
    {
        var fileInfo = _appDataManager.GetFileInfo(Path.Combine("Groups", path, fileName));
        if (fileInfo.Exists)
            return await System.IO.File.ReadAllBytesAsync(fileInfo.FullName);
        else
        {
            byte[]? file = await _fileIOHelper.GetFileAsync(Path.Combine("Groups", path), fileName, null);
            if (file != null)
            {
                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                await System.IO.File.WriteAllBytesAsync(fileInfo.FullName, file);
            }

            return file;
        }
    }

    /// <summary>
    /// 用于用户信息的文件获取
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetFile(string Id, string path, string fileName, FileTarget fileTarget)
    {
        var basePath = fileTarget switch
        {
            FileTarget.Group => "Groups",
            FileTarget.User => "Users"
        };
        var actualPath = Path.Combine(basePath, Id, path);

        var fileInfo = _appDataManager.GetFileInfo(Path.Combine(actualPath, fileName));
        if (fileInfo.Exists)
            return await System.IO.File.ReadAllBytesAsync(fileInfo.FullName);
        else
        {
            byte[]? file = await _fileIOHelper.GetFileAsync(actualPath, fileName, null);
            if (file != null)
            {
                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                await System.IO.File.WriteAllBytesAsync(fileInfo.FullName, file);
            }

            return file;
        }
    }


    /// <summary>
    /// 用于用户信息的文件保存
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public async Task<bool> UploadFile(string id, string path, string fileName, byte[] bytes,
        FileTarget fileTarget)
    {
        var basePath = fileTarget switch
        {
            FileTarget.Group => "Groups",
            FileTarget.User => "Users"
        };

        var actualPath = Path.Combine(basePath, id, path);
        var result = await _fileIOHelper.UploadFileAsync(actualPath, fileName, bytes);

        // 如果上传失败，返回false
        if (!result) return false;

        // 上传成功，保存到本地 
        var fileInfo = _appDataManager.GetFileInfo(Path.Combine(actualPath, fileName));
        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            Directory.CreateDirectory(fileInfo.DirectoryName!);
        await System.IO.File.WriteAllBytesAsync(fileInfo.FullName, bytes);
        return true;
    }

    public async Task<bool> SaveAsFile(string id, string path, string fileName, string filePath, FileTarget fileTarget)
    {
        var basePath = fileTarget switch
        {
            FileTarget.Group => "Groups",
            FileTarget.User => "Users"
        };

        var actualPath = Path.Combine(basePath, id, path);
        var fileInfo = _appDataManager.GetFileInfo(Path.Combine(actualPath, fileName));
        if (fileInfo.Exists)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                fileStream.Write(System.IO.File.ReadAllBytes(fileInfo.FullName));
            }

            return true;
        }
        else
            return await _fileIOHelper.DownloadLargeFileAsync(actualPath, fileName, filePath, new Progress<double>());
    }
}