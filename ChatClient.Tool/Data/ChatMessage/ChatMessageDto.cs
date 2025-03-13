using Avalonia.Media.Imaging;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Data;

public class ChatMessageDto
{
    public ChatMessage.ContentOneofCase Type { get; set; }

    public object Content { get; set; }
}

public class TextMessDto
{
    public string Text { get; set; }
}

public class ImageMessDto : BindableBase
{
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    private Bitmap? imageSource;

    public Bitmap? ImageSource
    {
        get => imageSource;
        set => SetProperty(ref imageSource, value);
    }
}

public class FileMessDto : BindableBase
{
    public int ChatId { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileType { get; set; }

    #region Property

    private bool _isDownload = false;

    public bool IsDownload
    {
        get => _isDownload;
        set => SetProperty(ref _isDownload, value);
    }

    // 记录另存地址
    private string? _targetFilePath;

    public string? TargetFilePath
    {
        get => _targetFilePath;
        set => SetProperty(ref _targetFilePath, value);
    }

    private FileProcessDto? _fileProcessDto;

    public FileProcessDto? FileProcessDto
    {
        get => _fileProcessDto;
        set => SetProperty(ref _fileProcessDto, value);
    }

    #endregion
}

public class SystemMessDto : BindableBase
{
    public List<SystemMessBlockDto> Blocks { get; set; } = new();
}

public class SystemMessBlockDto
{
    public string Text { get; set; }
    public bool Bold { get; set; }
}