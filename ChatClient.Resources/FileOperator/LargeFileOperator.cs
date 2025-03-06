using ChatClient.Tool.Data.File;
using File.Protobuf;

namespace ChatClient.Resources.FileOperator;

public class LargeFileOperator
{
    private readonly string _filePath;
    private readonly string _fileName;

    private FileStream? _fileStream;
    private FileUnit? _fileUnit;

    public event Action<bool> OnFileDownloadFinished;

    public LargeFileOperator(string filePath, string fileName)
    {
        _filePath = filePath;
        _fileName = fileName;
    }

    /// <summary>
    /// 接受到文件头
    /// </summary>
    /// <param name="fileHeader"></param>
    public bool ReceiveFileHeader(FileHeader fileHeader)
    {
        if (!fileHeader.Exist || !fileHeader.FileName.Equals(_fileName))
        {
            OnFileDownloadFinished?.Invoke(false);
            return false;
        }

        _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
        _fileUnit = new FileUnit(fileHeader);
        return true;
    }

    /// <summary>
    /// 接受到文件包
    /// </summary>
    /// <param name="filePack"></param>
    public bool ReceiveFilePack(FilePack filePack)
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
            System.IO.File.Delete(_filePath);
            OnFileDownloadFinished?.Invoke(false);
            return false;
        }

        _fileUnit.CurrentIndex = filePack.PackIndex;

        if (_fileStream != null)
        {
            _fileStream.Write(filePack.Data.ToByteArray(), 0, filePack.PackSize);
            _fileStream.Flush();
        }
        else
        {
            System.IO.File.Delete(_filePath);
            OnFileDownloadFinished?.Invoke(false);
            return false;
        }

        if (_fileUnit.CurrentIndex == _fileUnit.TotleCount)
        {
            FileInfo fileInfo = new FileInfo(_filePath);
            if (fileInfo.Length != _fileUnit.TotleSize)
            {
                _fileUnit.IsFinished = true;
                OnFileDownloadFinished?.Invoke(false);

                if (_fileStream != null)
                {
                    _fileStream.Dispose();
                    _fileStream = null;
                }

                System.IO.File.Delete(_filePath);
            }
            else
            {
                if (_fileStream != null)
                {
                    _fileStream.Dispose();
                    _fileStream = null;
                }

                _fileUnit.IsFinished = true;
                OnFileDownloadFinished?.Invoke(true);
            }
        }

        return true;
    }

    public void Clear()
    {
        if (_fileStream != null)
        {
            _fileStream.Dispose();
            _fileStream = null;
        }

        if (_fileUnit != null)
            _fileUnit.Clear();
    }
}