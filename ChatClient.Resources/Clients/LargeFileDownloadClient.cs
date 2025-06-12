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

    public LargeFileDownloadClient(IChannel channel)
    {
        _channel = new WeakReference<IChannel>(channel);
    }

    public async Task<bool> RequestFile(FileRequest message, string filePath, IProgress<double> fileProgress)
    {
        if (!_channel.TryGetTarget(out var channel))
            throw new NullReferenceException();
        
        if(!channel.Active)
            throw new Exception("连接未激活，请检查连接状态");

        try
        {
            // 创建大文件处理器
            _largeFileOperator = new LargeFileOperator(filePath, message.FileName, fileProgress);
            _largeFileOperator.OnFileDownloadFinished += OnFileDownloadFinished;

            // 添加channel管道的handler
            channel.Pipeline.AddLast(new LargeFileDownloadServerHandler(this));

            // 等待文件接受完毕或者出错
            taskCompletionSourceOfFileUnit = new TaskCompletionSource<bool>();

            // 使用超时保护
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20))) // 大文件可能需要更长时间
            {
                // 注册取消回调
                cts.Token.Register(() =>
                {
                    taskCompletionSourceOfFileUnit?.TrySetCanceled();
                    Clear();
                });

                // 写入文件请求
                await channel.WriteAndFlushProtobufAsync(message);


                await taskCompletionSourceOfFileUnit.Task;

                if (taskCompletionSourceOfFileUnit.Task.IsCanceled)
                    return false;
                else
                    return taskCompletionSourceOfFileUnit.Task.Result;
            }
        }
        catch (Exception)
        {
            Clear();
            throw;
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
        // 开始读取文件
        FilePackResponse response = new FilePackResponse
        {
            Success = result ?? false,
            FileName = fileHeader.FileName,
            PackIndex = 0,
            Time = fileHeader.Time,
            PackSize = 0
        };
        if (_channel.TryGetTarget(out var channel))
            await channel.WriteAndFlushProtobufAsync(response);
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

        // 确保未完成任务被取消
        if (taskCompletionSourceOfFileUnit != null && !taskCompletionSourceOfFileUnit.Task.IsCompleted)
        {
            taskCompletionSourceOfFileUnit.TrySetCanceled();
        }

        taskCompletionSourceOfFileUnit = null;
    }
}