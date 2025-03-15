using ChatClient.Tool.Data.File;
using File.Protobuf;
using System;
using System.Buffers; // 添加引用
using System.IO;

namespace ChatClient.Resources.FileOperator;

public class LargeFileOperator : IDisposable
{
    private readonly string _filePath;
    private readonly string _fileName;
    private readonly int _bufferSize = 65536; // 64KB，与服务器分片大小匹配
    private readonly int _flushThreshold = 4194304; // 4MB，约64个分片后刷新
    private long _bytesWrittenSinceFlush = 0;

    private FileStream? _fileStream;
    private BufferedStream? _bufferedStream; // 添加缓冲流
    private FileUnit? _fileUnit;
    private bool _disposed;

    public event Action<bool>? OnFileDownloadFinished;

    public LargeFileOperator(string filePath, string fileName)
    {
        _filePath = filePath;
        _fileName = fileName;
        _disposed = false;
    }

    /// <summary>
    /// 接受到文件头
    /// </summary>
    /// <param name="fileHeader"></param>
    public bool ReceiveFileHeader(FileHeader fileHeader)
    {
        ThrowIfDisposed();

        try
        {
            if (!fileHeader.Exist || !fileHeader.FileName.Equals(_fileName))
            {
                OnFileDownloadFinished?.Invoke(false);
                return false;
            }

            // 关闭现有流
            CloseStream();

            // 确保目录存在
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 使用缓冲流提高性能
            _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                _bufferSize, FileOptions.SequentialScan);
            _bufferedStream = new BufferedStream(_fileStream, _bufferSize * 4);
            _fileUnit = new FileUnit(fileHeader);
            _bytesWrittenSinceFlush = 0;
            return true;
        }
        catch (Exception ex)
        {
            CloseStream();
            OnFileDownloadFinished?.Invoke(false);
            // 考虑记录异常
            return false;
        }
    }

    /// <summary>
    /// 接受到文件包
    /// </summary>
    /// <param name="filePack"></param>
    public bool ReceiveFilePack(FilePack filePack)
    {
        ThrowIfDisposed();

        try
        {
            if (_fileUnit == null)
            {
                OnFileDownloadFinished?.Invoke(false);
                return false;
            }

            if (!filePack.FileName.Equals(_fileName) || _fileUnit.CurrentIndex + 1 != filePack.PackIndex)
            {
                // 丢包
                _fileUnit.IsFinished = true;
                CloseStream();
                SafeDeleteFile();
                OnFileDownloadFinished?.Invoke(false);
                return false;
            }

            _fileUnit.CurrentIndex = filePack.PackIndex;

            if (_bufferedStream != null)
            {
                try
                {
                    // 使用共享内存池来减少GC压力
                    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(filePack.PackSize);
                    try
                    {
                        // 直接复制到租用的缓冲区
                        filePack.Data.CopyTo(sharedBuffer, 0);
                        _bufferedStream.Write(sharedBuffer, 0, filePack.PackSize);

                        // 累积写入量
                        _bytesWrittenSinceFlush += filePack.PackSize;

                        // 只在达到阈值或是最后一个数据包时刷新
                        if (_bytesWrittenSinceFlush >= _flushThreshold ||
                            _fileUnit.CurrentIndex == _fileUnit.TotleCount)
                        {
                            _bufferedStream.Flush();
                            _bytesWrittenSinceFlush = 0;
                        }
                    }
                    finally
                    {
                        // 归还缓冲区
                        ArrayPool<byte>.Shared.Return(sharedBuffer);
                    }
                }
                catch (Exception)
                {
                    CloseStream();
                    SafeDeleteFile();
                    OnFileDownloadFinished?.Invoke(false);
                    return false;
                }
            }
            else
            {
                SafeDeleteFile();
                OnFileDownloadFinished?.Invoke(false);
                return false;
            }

            if (_fileUnit.CurrentIndex == _fileUnit.TotleCount)
            {
                // 确保所有数据都写入磁盘
                _bufferedStream?.Flush();

                FileInfo fileInfo = new FileInfo(_filePath);
                if (fileInfo.Length != _fileUnit.TotleSize)
                {
                    _fileUnit.IsFinished = true;
                    CloseStream();
                    SafeDeleteFile();
                    OnFileDownloadFinished?.Invoke(false);
                }
                else
                {
                    CloseStream();
                    _fileUnit.IsFinished = true;
                    OnFileDownloadFinished?.Invoke(true);
                }
            }

            return true;
        }
        catch (Exception)
        {
            CloseStream();
            SafeDeleteFile();
            OnFileDownloadFinished?.Invoke(false);
            return false;
        }
    }

    private void CloseStream()
    {
        if (_bufferedStream != null)
        {
            _bufferedStream.Dispose();
            _bufferedStream = null;
        }

        if (_fileStream != null)
        {
            _fileStream.Dispose();
            _fileStream = null;
        }
    }

    private void SafeDeleteFile()
    {
        try
        {
            if (System.IO.File.Exists(_filePath))
            {
                System.IO.File.Delete(_filePath);
            }
        }
        catch (Exception)
        {
            // 记录但不抛出异常
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LargeFileOperator));
    }

    public void Clear()
    {
        CloseStream();

        if (_fileUnit != null)
        {
            _fileUnit.Clear();
            _fileUnit = null;
        }

        OnFileDownloadFinished = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Clear();
        }

        _disposed = true;
    }

    ~LargeFileOperator()
    {
        Dispose(false);
    }
}