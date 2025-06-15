using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

public class ImageManager : IImageManager
{
    private readonly IContainerProvider _containerProvider;
    private readonly IFileOperateHelper _fileOperateHelper;

    private readonly ConcurrentDictionary<string, Bitmap> _imageCache = new();
    private readonly ConcurrentDictionary<string, Bitmap> _chatImageCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _chatImageLastAccessTime = new();
    private readonly ConcurrentDictionary<string, Bitmap> _staticImageCache = new();

    private SemaphoreSlim _semaphoreSlim1 = new(1, 1);
    private SemaphoreSlim _semaphoreSlim2 = new(1, 1);
    private SemaphoreSlim _semaphoreSlim3 = new(1, 1);
    private SemaphoreSlim _semaphoreSlim4 = new(1, 1);

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

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetGroupFile(path, fileName))
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

    // 获取头像资源
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

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetFile(id, path, fileName, fileTarget))
                bitmap = new Bitmap(stream);

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

    public async Task<Bitmap?> GetChatFile(string id, string path, string fileName, FileTarget fileTarget)
    {
        // 先检查是否存在缓存
        string cacheKey = $"{fileTarget}_{id}_{path}_{fileName}";
        if (_chatImageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
        {
            _chatImageLastAccessTime[cacheKey] = DateTime.Now;
            return cachedBitmap;
        }

        try
        {
            await _semaphoreSlim3.WaitAsync();

            if (_chatImageCache.TryGetValue(cacheKey, out cachedBitmap))
            {
                _chatImageLastAccessTime[cacheKey] = DateTime.Now;
                return cachedBitmap;
            }

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetFile(id, path, fileName, fileTarget))
                bitmap = new Bitmap(stream);

            // 添加bitmap到字典中缓存
            _chatImageCache.TryAdd(cacheKey, bitmap);
            // 记录最后访问时间
            _chatImageLastAccessTime.TryAdd(cacheKey, DateTime.Now);

            return bitmap;
        }
        catch (Exception e)
        {
            return null;
        }
        finally
        {
            _semaphoreSlim3.Release();
        }
    }

    /// <summary>
    /// 清理长时间未使用的聊天图片缓存
    /// </summary>
    /// <param name="maxAgeMinutes">清理超过此分钟数未访问的图片，默认30分钟</param>
    /// <returns>清理的图片数量</returns>
    public int CleanupUnusedChatImages(int maxAgeMinutes = 10)
    {
        int removedCount = 0;
        DateTime cutoffTime = DateTime.Now.AddMinutes(-maxAgeMinutes);

        try
        {
            _semaphoreSlim3.Wait();

            List<string> keysToRemove = _chatImageLastAccessTime
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_chatImageCache.TryRemove(key, out Bitmap? bitmap))
                {
                    bitmap.Dispose();
                    _chatImageLastAccessTime.TryRemove(key, out _);
                    removedCount++;
                }
            }
        }
        finally
        {
            _semaphoreSlim3.Release();
        }

        return removedCount;
    }

    public async Task<Bitmap> GetStaticFile(string path)
    {
        string cacheKey = $"static_{path}";
        if (_staticImageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        try
        {
            await _semaphoreSlim4.WaitAsync();
            if (_staticImageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            if (!System.IO.File.Exists(path)) return null;

            Bitmap bitmap;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                bitmap = new Bitmap(stream);

            // 添加bitmap到字典中缓存
            _staticImageCache.TryAdd(cacheKey, bitmap);

            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphoreSlim4.Release();
        }
    }

    public void ClearCache()
    {
        foreach (var image in _imageCache.Values)
            image.Dispose();
        _imageCache.Clear();

        foreach (var image in _chatImageCache.Values)
            image.Dispose();
        _chatImageCache.Clear();
        _chatImageLastAccessTime.Clear();

        foreach (var image in _staticImageCache.Values)
            image.Dispose();
        _staticImageCache.Clear();
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