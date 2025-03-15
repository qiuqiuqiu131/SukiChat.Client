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

        try
        {
            // 存储文件下载进度
            _fileProcessDto = fileProcessDto;

            // 创建大文件处理器
            _largeFileOperator = new LargeFileOperator(filePath, message.FileName);
            _largeFileOperator.OnFileDownloadFinished += OnFileDownloadFinished;

            // 添加channel管道的handler
            channel.Pipeline.AddLast(new LargeFileDownloadServerHandler(this));

            // 等待文件接受完毕或者出错
            taskCompletionSourceOfFileUnit = new TaskCompletionSource<bool>();

            // 写入文件请求
            await channel.WriteAndFlushProtobufAsync(message);

            // 使用超时保护
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20))) // 大文件可能需要更长时间
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
                var completedTask = await Task.WhenAny(taskCompletionSourceOfFileUnit.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Clear();
                    throw new TimeoutException("大文件下载超时");
                }

                // 结束文件下载进度
                if (_fileProcessDto != null)
                    _fileProcessDto.CurrentSize = _fileProcessDto.MaxSize;

                // 获取结果
                return taskCompletionSourceOfFileUnit.Task.Result;
            }
        }
        catch (Exception)
        {
            Clear();
            throw;
        }
        finally
        {
            _fileProcessDto = null;
        }
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
        {
            // 取消事件订阅
            _largeFileOperator.OnFileDownloadFinished -= OnFileDownloadFinished;
            _largeFileOperator.Clear();
            _largeFileOperator = null;
        }

        _fileProcessDto = null;

        // 确保未完成任务被取消
        if (taskCompletionSourceOfFileUnit != null && !taskCompletionSourceOfFileUnit.Task.IsCompleted)
        {
            taskCompletionSourceOfFileUnit.TrySetCanceled();
        }

        taskCompletionSourceOfFileUnit = null;
    }
}