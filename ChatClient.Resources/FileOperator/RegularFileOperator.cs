using ChatClient.Tool.Data.File;
using File.Protobuf;

namespace ChatClient.Resources.FileOperator;

public class RegularFileOperator : IDisposable
{
    private readonly string _fileName;
    private FileUnit? _fileUnit;
    private bool _disposed = false;

    public event Action<FileUnit> OnFileDownloadFinished;

    public RegularFileOperator(string fileName)
    {
        _fileName = fileName;
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
            if (!filePack.FileName.Equals(_fileName) || _fileUnit == null)
            {
                if (_fileUnit != null)
                    _fileUnit.Clear();
                OnFileDownloadFinished?.Invoke(null);
                return false;
            }

            var result = _fileUnit.AddPack(filePack.PackIndex, filePack.Data);

            if (_fileUnit.IsFinished)
            {
                var completedUnit = _fileUnit;
                OnFileDownloadFinished?.Invoke(completedUnit);
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
}