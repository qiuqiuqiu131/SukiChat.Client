using System.Net.Http.Headers;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using Microsoft.Extensions.Configuration;

namespace ChatClient.BaseService.Helper;

internal class WebApiFileIOHelper : IFileIOHelper
{
    private readonly string baseUrl;

    public WebApiFileIOHelper(IConfigurationRoot configuration)
    {
        baseUrl = configuration["WebApiUrl"]!;
    }

    public async Task<bool> UploadFileAsync(string targetPath, string fileName, byte[] file)
    {
        try
        {
            HttpClient client = new HttpClient();

            string actualPath = targetPath.Replace("\\", "_").Replace("/", "_");
            string url = $"{baseUrl}/file/upload/{actualPath}";


            using var content = new MultipartFormDataContent();
            using var fileStream = new MemoryStream(file);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            // 发送Http请求
            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                Console.WriteLine($"上传失败: {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UploadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress)
    {
        try
        {
            HttpClient client = new HttpClient();

            string actualPath = targetPath.Replace("\\", "_").Replace("/", "_");
            string url = $"{baseUrl}/file/upload/{actualPath}";


            using var content = new MultipartFormDataContent();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            // 发送Http请求
            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                Console.WriteLine($"上传失败: {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
            return false;
        }
    }

    public async Task<byte[]?> GetFileAsync(string targetPath, string fileName, IProgress<double> fileProgress)
    {
        return await GetFileBaseAsync("download", targetPath, fileName);
    }

    public Task<bool> DownloadLargeFileAsync(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]?> GetCompressedFileAsync(string targetPath, string fileName)
    {
        return await GetFileBaseAsync("download/compressed", targetPath, fileName);
    }

    private async Task<byte[]?> GetFileBaseAsync(string mUrl, string targetPath, string fileName)
    {
        try
        {
            HttpClient client = new HttpClient();

            string actualPath = targetPath.Replace("\\", "_");
            string url = $"{baseUrl}/file/{mUrl}/{actualPath}/{fileName}";

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                return fileBytes;
            }
            else
            {
                Console.WriteLine($"下载失败: {response.ReasonPhrase}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
            return null;
        }
    }
}