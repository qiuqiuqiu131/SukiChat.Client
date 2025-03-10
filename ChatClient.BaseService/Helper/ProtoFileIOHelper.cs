using System.Net;
using ChatClient.Resources;
using ChatClient.Resources.Clients;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common;
using File.Protobuf;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;

namespace ChatClient.BaseService.Helper;

public class ProtoFileIOHelper : IFileIOHelper
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IResourcesClientPool _resourcesClientPool;
    private IFileIOHelper _fileIoHelperImplementation;

    public ProtoFileIOHelper(IConfigurationRoot configurationRoot, IResourcesClientPool resourcesClientPool)
    {
        _configurationRoot = configurationRoot;
        _resourcesClientPool = resourcesClientPool;
    }

    /// <summary>
    /// 使用Socket上传文件
    /// </summary>
    /// <param name="targetPath"></param>
    /// <param name="fileName"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<bool> UploadFileAsync(string targetPath, string fileName, byte[] file)
    {
        try
        {
            var client = await _resourcesClientPool.GetClientAsync<RegularFileUploadClient>();

            // 开启两个线程同时执行
            var timeoutTask = Task.Delay(10000000);
            var uploadTask = client.UploadFile(targetPath, fileName, file);
            // 等待其中一个线程执行完成
            var completedTask = await Task.WhenAny(uploadTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                _resourcesClientPool.ReturnClient(client);
                throw new TimeoutException($"获取文件请求超时（10秒）{client._fileStream}");
            }

            var result = uploadTask.Result;
            _resourcesClientPool.ReturnClient(client);

            return result?.Success ?? false;
        }
        catch (Exception e)
        {
            // 异常处理
            return false;
        }
    }

    public async Task<bool> UploadLargeFileAsync(string targetPath, string fileName, string filePath,
        FileProcessDto? fileProcessDto = null)
    {
        try
        {
            var client = await _resourcesClientPool.GetClientAsync<LargeFileUploadClient>();
            await client.UploadFile(targetPath, fileName, filePath, fileProcessDto);
            _resourcesClientPool.ReturnClient(client);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 通过Socket下载文件
    /// </summary>
    /// <param name="targetPath"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetFileAsync(string targetPath, string fileName)
    {
        try
        {
            var client = await _resourcesClientPool.GetClientAsync<RegularFileDownloadClient>();
            var request = new FileRequest
            {
                Path = targetPath,
                FileName = fileName,
                Type = Path.GetExtension(fileName)
            };

            var fileProcessDto = new FileProcessDto { FileName = fileName };
            var fileUnit = await client.RequestFile(request, fileProcessDto);

            var result = fileUnit.Combine();
            _resourcesClientPool.ReturnClient(client);
            return result;
        }
        catch (Exception e)
        {
            //TODO: 异常处理
            return null;
        }
    }

    public async Task<bool> DownloadLargeFileAsync(string targetPath, string fileName, string filePath,
        FileProcessDto? fileProcessDto = null)
    {
        try
        {
            var client = await _resourcesClientPool.GetClientAsync<LargeFileDownloadClient>();
            var request = new FileRequest
            {
                Path = targetPath,
                FileName = fileName,
                Type = Path.GetExtension(fileName)
            };
            if (fileProcessDto != null)
                fileProcessDto.FileName = fileName;

            // 等待文件请求
            var result = await client.RequestFile(request, filePath, fileProcessDto);

            // 归还连接
            _resourcesClientPool.ReturnClient(client);

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<byte[]?> GetCompressedFileAsync(string targetPath, string fileName)
    {
        return null;
    }
}