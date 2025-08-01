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

    // 统一的锁字典，按 cacheKey 管理
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public ImageManager(IContainerProvider containerProvider, IFileOperateHelper fileOperateHelper)
    {
        _containerProvider = containerProvider;
        _fileOperateHelper = fileOperateHelper;
    }

    // 获取或创建指定 cacheKey 的锁
    private SemaphoreSlim GetOrCreateLock(string cacheKey)
    {
        return _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<Bitmap?> GetGroupFile(string path, string fileName)
    {
        string cacheKey = $"group_{path}_{fileName}";
        if (_imageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        var semaphore = GetOrCreateLock(cacheKey);
        try
        {
            await semaphore.WaitAsync();

            if (_imageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetGroupFile(path, fileName))
                bitmap = new Bitmap(stream);

            _imageCache.TryAdd(cacheKey, bitmap);
            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<Bitmap?> GetFile(string id, string path, string fileName, FileTarget fileTarget)
    {
        string cacheKey = $"{fileTarget}_{id}_{path}_{fileName}";
        if (_imageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        var semaphore = GetOrCreateLock(cacheKey);
        try
        {
            await semaphore.WaitAsync();

            if (_imageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetFile(id, path, fileName, fileTarget))
                bitmap = new Bitmap(stream);

            _imageCache.TryAdd(cacheKey, bitmap);
            return bitmap;
        }
        catch (Exception e)
        {
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<Bitmap?> GetChatFile(string id, string path, string fileName, FileTarget fileTarget)
    {
        string cacheKey = $"{fileTarget}_{id}_{path}_{fileName}";
        if (_chatImageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
        {
            _chatImageLastAccessTime[cacheKey] = DateTime.Now;
            return cachedBitmap;
        }

        var semaphore = GetOrCreateLock(cacheKey);
        try
        {
            await semaphore.WaitAsync();

            if (_chatImageCache.TryGetValue(cacheKey, out cachedBitmap))
            {
                _chatImageLastAccessTime[cacheKey] = DateTime.Now;
                return cachedBitmap;
            }

            Bitmap bitmap;
            await using (var stream = await _fileOperateHelper.GetFile(id, path, fileName, fileTarget))
                bitmap = new Bitmap(stream);

            _chatImageCache.TryAdd(cacheKey, bitmap);
            _chatImageLastAccessTime.TryAdd(cacheKey, DateTime.Now);

            return bitmap;
        }
        catch (Exception e)
        {
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public int CleanupUnusedChatImages(int maxAgeMinutes = 10)
    {
        int removedCount = 0;
        DateTime cutoffTime = DateTime.Now.AddMinutes(-maxAgeMinutes);

        List<string> keysToRemove = _chatImageLastAccessTime
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            var semaphore = GetOrCreateLock(key);
            semaphore.Wait();
            try
            {
                if (_chatImageCache.TryRemove(key, out Bitmap? bitmap))
                {
                    bitmap.Dispose();
                    _chatImageLastAccessTime.TryRemove(key, out _);
                    removedCount++;
                }
            }
            finally
            {
                semaphore.Release();
                // 清理不再需要的锁
                _locks.TryRemove(key, out _);
            }
        }

        return removedCount;
    }

    public async Task<Bitmap> GetStaticFile(string path)
    {
        string cacheKey = $"static_{path}";
        if (_staticImageCache.TryGetValue(cacheKey, out Bitmap? cachedBitmap))
            return cachedBitmap;

        var semaphore = GetOrCreateLock(cacheKey);
        try
        {
            await semaphore.WaitAsync();
            if (_staticImageCache.TryGetValue(cacheKey, out cachedBitmap))
                return cachedBitmap;

            if (!System.IO.File.Exists(path)) return null;

            Bitmap bitmap;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                bitmap = new Bitmap(stream);

            _staticImageCache.TryAdd(cacheKey, bitmap);
            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            semaphore.Release();
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

        foreach (var l in _locks.Values)
            l.Dispose();
        _locks.Clear();
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