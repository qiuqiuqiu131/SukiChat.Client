using Avalonia.Media.Imaging;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Data;

public class ChatMessageDto : IDisposable
{
    public ChatMessage.ContentOneofCase Type { get; set; }

    public object Content { get; set; }

    public void Dispose()
    {
        if (Content is IDisposable disposable)
            disposable.Dispose();
        Content = null;
    }
}

public class TextMessDto
{
    public string Text { get; set; }
}

public class ImageMessDto : BindableBase, IDisposable
{
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    private Bitmap? imageSource;

    public string? ActualPath { get; set; }

    public Bitmap? ImageSource
    {
        get => imageSource;
        set => SetProperty(ref imageSource, value);
    }

    private bool failed = false;

    public bool Failed
    {
        get => failed;
        set => SetProperty(ref failed, value);
    }

    public void Dispose()
    {
        imageSource = null;
    }
}

public class VoiceMessDto : BindableBase, IDisposable
{
    public string? FilePath { get; set; }
    public long FileSize { get; set; }

    public byte[]? AudioData { get; set; }

    public TimeSpan Duration { get; set; }

    public string? ActualPath { get; set; }

    private bool failed = false;

    public bool Failed
    {
        get => failed;
        set => SetProperty(ref failed, value);
    }

    public void Dispose()
    {
        if (AudioData != null)
            Array.Clear(AudioData);
        AudioData = null;
    }
}

public class FileMessDto : BindableBase
{
    public bool IsUser { get; set; }
    public int ChatId { get; set; }

    private string fileName;

    public string FileName
    {
        get => fileName;
        set => SetProperty(ref fileName, value);
    }

    public long FileSize { get; set; }
    public string FileType { get; set; }

    #region Property

    // 文件是否下载成功，并且保存在本地
    private bool isSuccess = false;

    public bool IsSuccess
    {
        get => isSuccess;
        set => SetProperty(ref isSuccess, value);
    }

    // 文件是否存在在服务器上
    private bool isExit = false;

    public bool IsExit
    {
        get => isExit;
        set => SetProperty(ref isExit, value);
    }

    // 记录保存地址
    private string? _targetFilePath;

    public string? TargetFilePath
    {
        get => _targetFilePath;
        set => SetProperty(ref _targetFilePath, value);
    }

    private bool _isDownloading = false;

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    private double _downloadProgress = 0;

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    #endregion
}

public class SystemMessDto : BindableBase, IDisposable
{
    public List<SystemMessBlockDto> Blocks { get; set; } = new();

    public void Dispose()
    {
        Blocks.Clear();
    }
}

public class SystemMessBlockDto
{
    public string Text { get; set; }
    public bool Bold { get; set; }
}

public class CardMessDto
{
    public bool IsUser { get; set; }
    public string Id { get; set; }
    public object? Content { get; set; }

    public bool IsSelf { get; set; }

    public string SelfName => IsSelf ? "对方" : "你";

    public string Title => IsUser ? $"向{SelfName}推荐好友" : $"邀请{SelfName}加入群聊";

    public string Detail
    {
        get
        {
            if (Content == null) return "";
            return IsUser
                ? $"{Title} \"{((UserDto)Content).Name}\",点击查看具体信息"
                : $"{Title} \"{((GroupDto)Content).Name}\",点击查看具体信息";
        }
    }

    public string Bottom => IsUser ? "好友推荐" : "邀请入群";

    public Bitmap? HeadImage
    {
        get
        {
            if (Content == null) return null;
            return IsUser ? ((UserDto)Content).HeadImage : ((GroupDto)Content).HeadImage;
        }
    }
}