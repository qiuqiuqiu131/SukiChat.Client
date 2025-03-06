using ChatClient.Tool.Data.File;
using File.Protobuf;

namespace ChatClient.Resources.FileOperator;

public class RegularFileOperator
{
    private readonly string _fileName;
    private FileUnit? _fileUnit;

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
        if (!filePack.FileName.Equals(_fileName) || _fileUnit == null)
        {
            if (_fileUnit != null)
                _fileUnit.Clear();
            OnFileDownloadFinished?.Invoke(null);
            return false;
        }

        var result = _fileUnit.AddPack(filePack.PackIndex, filePack.Data);

        if (_fileUnit.IsFinished)
            OnFileDownloadFinished?.Invoke(_fileUnit);

        return result;
    }

    public void Clear()
    {
        if (_fileUnit != null)
            _fileUnit.Clear();
    }
}