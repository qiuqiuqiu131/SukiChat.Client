using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

public class ImageManager : IImageManager
{
    private readonly IContainerProvider _containerProvider;
    private readonly IFileOperateHelper _fileOperateHelper;
    private readonly ConcurrentDictionary<string, Bitmap> _imageCache = new();

    private SemaphoreSlim _semaphoreSlim1 = new(1, 1);
    private SemaphoreSlim _semaphoreSlim2 = new(1, 1);

    public ImageManager(IContainerProvider containerProvider, IFileOperateHelper fileOperateHelper)
    {
        _containerProvider = containerProvider;
        _fileOperateHelper = fileOperateHelper;
    }

    public async Task<Bitmap?> GetGroupFile(string path, string fileName)
    {
        // 先检查是否存在缓存
        string cacheKey = $"group_{path}_{fileName}";
        if (_imageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        try
        {
            await _semaphoreSlim1.WaitAsync();

            if (_imageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            var bytes = await _fileOperateHelper.GetGroupFile(path, fileName);
            if (bytes == null) return null;

            Bitmap bitmap;
            using (var stream = new MemoryStream(bytes))
                bitmap = new Bitmap(stream);

            // 添加bitmap到字典中缓存
            _imageCache.TryAdd(cacheKey, bitmap);

            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphoreSlim1.Release();
        }
    }

    public async Task<Bitmap?> GetFile(string id, string path, string fileName, FileTarget fileTarget)
    {
        // 先检查是否存在缓存
        string cacheKey = $"{fileTarget}_{id}_{path}_{fileName}";
        if (_imageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        try
        {
            await _semaphoreSlim2.WaitAsync();

            if (_imageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            var bytes = await _fileOperateHelper.GetFile(id, path, fileName, fileTarget);
            if (bytes == null) return null;

            Bitmap bitmap;
            using (var stream = new MemoryStream(bytes))
                bitmap = new Bitmap(stream);

            Array.Clear(bytes);

            // 添加bitmap到字典中缓存
            _imageCache.TryAdd(cacheKey, bitmap);

            return bitmap;
        }
        catch (Exception e)
        {
            return null;
        }
        finally
        {
            _semaphoreSlim2.Release();
        }
    }

    public void ClearCache()
    {
        foreach (var image in _imageCache.Values)
            image.Dispose();
        _imageCache.Clear();
    }

    public bool RemoveFromCache(string id, string path, string fileName, FileTarget? fileTarget = null)
    {
        string cacheKey;
        if (fileTarget.HasValue)
            cacheKey = $"{fileTarget}_{id}_{path}_{fileName}";
        else
            cacheKey = $"group_{path}_{fileName}";

        return _imageCache.TryRemove(cacheKey, out _);
    }
}