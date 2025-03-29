using ChatClient.Tool.Data.File;
using File.Protobuf;

namespace ChatClient.Resources.FileOperator;

public class RegularFileOperator : IDisposable
{
    private readonly string _fileName;
    private readonly IProgress<double>? _fileProgress;

    private FileUnit? _fileUnit;

    private bool _disposed = false;

    public event Action<FileUnit?> OnFileDownloadFinished;

    public RegularFileOperator(string fileName, IProgress<double>? fileProgress)
    {
        _fileName = fileName;
        _fileProgress = fileProgress;
    }

    /// <summary>
    /// 接受到文件头
    /// </summary>
    /// <param name="fileHeader"></param>
    public bool ReceiveFileHeader(FileHeader fileHeader)
    {
        if (!fileHeader.FileName.Equals(_fileName) || !fileHeader.Exist)
        {
            OnFileDownloadFinished?.Invoke(null);
            return false;
        }

        _fileUnit = new FileUnit(fileHeader);
        return true;
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
            // 文件名不匹配或者文件单元为空
            if (!filePack.FileName.Equals(_fileName) || _fileUnit == null)
            {
                _fileUnit?.Clear();
                OnFileDownloadFinished?.Invoke(null);
                return false;
            }

            // 添加文件包
            var result = _fileUnit.AddPack(filePack.PackIndex, filePack.Data);
            // 更新进度
            _fileProgress?.Report((double)_fileUnit!.CurrentIndex / _fileUnit!.TotleCount);

            if (_fileUnit.IsFinished)
            {
                _fileProgress?.Report(1);
                OnFileDownloadFinished?.Invoke(_fileUnit);
            }

            return result;
        }
        catch (Exception)
        {
            if (_fileUnit != null)
            {
                _fileUnit.Clear();
                _fileUnit = null;
            }

            OnFileDownloadFinished?.Invoke(null);
            return false;
        }
    }

    public void Clear()
    {
        if (_fileUnit != null)
        {
            _fileUnit.Clear();
            _fileUnit = null;
        }

        OnFileDownloadFinished = null;
    }

    #region Dispose

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RegularFileOperator));
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

    ~RegularFileOperator()
    {
        Dispose(false);
    }

    #endregion
}