using ChatClient.Resources.FileOperator;
using ChatClient.ResourcesClient.ServerHandlers;
using ChatClient.Tool.Data.File;
using ChatServer.Common;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.Clients;

public class LargeFileDownloadClient : IFileClient
{
    private readonly WeakReference<IChannel> _channel;
    private TaskCompletionSource<bool>? taskCompletionSourceOfFileUnit;

    private LargeFileOperator? _largeFileOperator;

    // 用于记录当前文件上传下载的信息
    private FileProcessDto? _fileProcessDto;

    public LargeFileDownloadClient(IChannel channel)
    {
        _channel = new WeakReference<IChannel>(channel);
    }

    public async Task<bool> RequestFile(FileRequest message, string filePath, FileProcessDto? fileProcessDto = null)
    {
        if (!_channel.TryGetTarget(out var channel))
            throw new NullReferenceException();

        // 存储文件下载进度
        _fileProcessDto = fileProcessDto;

        // 创建大文件处理器
        _largeFileOperator = new LargeFileOperator(filePath, message.FileName);
        _largeFileOperator.OnFileDownloadFinished += OnFileDownloadFinished;

        // 添加channel管道的handler
        channel.Pipeline.AddLast(new LargeFileDownloadServerHandler(this));

        // 写入文件请求
        await channel.WriteAndFlushProtobufAsync(message);

        // 等待文件接受完毕或者出错
        taskCompletionSourceOfFileUnit = new TaskCompletionSource<bool>();
        await taskCompletionSourceOfFileUnit.Task;

        // 结束文件下载进度
        if (_fileProcessDto != null)
            _fileProcessDto.CurrentSize = _fileProcessDto.MaxSize;
        _fileProcessDto = null;

        // 返回结果
        return taskCompletionSourceOfFileUnit.Task.Result;
    }

    /// <summary>
    /// 文件上传完成回调
    /// </summary>
    /// <param name="response"></param>
    public void OnFileDownloadFinished(bool result)
    {
        if (taskCompletionSourceOfFileUnit == null) return;
        taskCompletionSourceOfFileUnit.SetResult(result);
    }

    /// <summary>
    /// 接收到文件头回调
    /// </summary>
    /// <param name="fileHeader"></param>
    public async void OnFileHeaderReceived(FileHeader fileHeader)
    {
        var result = _largeFileOperator?.ReceiveFileHeader(fileHeader);
        if (result ?? false)
        {
            // 开始读取文件
            FilePackResponse response = new FilePackResponse
            {
                Success = true,
                FileName = fileHeader.FileName,
                PackIndex = 0,
                Time = fileHeader.Time,
                PackSize = 0
            };
            if (_channel.TryGetTarget(out var channel))
                await channel.WriteAndFlushProtobufAsync(response);
        }
    }

    /// <summary>
    /// 接收到文件分片回调
    /// </summary>
    /// <param name="filePack"></param>
    public async void OnNewFilePackReceived(FilePack filePack)
    {
        var result = _largeFileOperator?.ReceiveFilePack(filePack);
        FilePackResponse response = new FilePackResponse
        {
            Success = result ?? false,
            FileName = filePack.FileName,
            PackIndex = filePack.PackIndex,
            Time = filePack.Time,
            PackSize = filePack.PackSize
        };
        if ((result ?? false) && _fileProcessDto != null)
            _fileProcessDto.CurrentSize += filePack.PackSize;

        // 发送文件分片响应
        if (_channel.TryGetTarget(out var channel))
            await channel.WriteAndFlushProtobufAsync(response);
    }

    public IChannel? ReturnChannel()
    {
        if (_channel.TryGetTarget(out var channel))
        {
            channel.Pipeline.Remove<LargeFileDownloadServerHandler>();
            return channel;
        }

        return null;
    }

    public void Clear()
    {
        if (_largeFileOperator != null)
            _largeFileOperator.Clear();

        _fileProcessDto = null;
    }
}