using System.Net;
using ChatClient.Resources.FileOperator;
using ChatClient.Resources.ServerHandlers;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Events;
using ChatServer.Common;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.Clients;

/// <summary>
/// 连接资源服务器，短连接，构造时提供EndPoint，Send时构造消息，发送请求，接收响应
/// </summary>
public class RegularFileDownloadClient : IFileClient
{
    private readonly WeakReference<IChannel> _channel;
    private TaskCompletionSource<FileUnit?>? taskCompletionSourceOfFileUnit;

    private RegularFileOperator _regularFileOperator;

    public RegularFileDownloadClient(IChannel channel)
    {
        _channel = new WeakReference<IChannel>(channel);
    }

    #region DownLoad

    /// <summary>
    /// 下载文件
    /// 请使用try-catch捕获异常
    /// 1、连接超时会抛出ConnectTimeoutException
    /// 2、连接对象未初始化会抛出NullReferenceException
    /// </summary>
    /// <param name="message">可传入不同的文件请求消息</param>
    /// <param name="fileProcessDto">可传入FileProcessDto，用于记录文件下载进度</param>
    public async Task<FileUnit?> RequestFile(FileRequest message, IProgress<double>? fileProgress)
    {
        if (!_channel.TryGetTarget(out var channel))
            throw new NullReferenceException();

        try
        {
            // 创建文件处理器
            _regularFileOperator = new RegularFileOperator(message.FileName, fileProgress);
            _regularFileOperator.OnFileDownloadFinished += OnFileDownloadFinished;

            // 添加channel管道的handler
            channel.Pipeline.AddLast(new RegularFileDownloadServerHandler(this));

            // 等待文件接受完毕或者出错
            taskCompletionSourceOfFileUnit = new TaskCompletionSource<FileUnit?>();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                // 注册取消回调
                cts.Token.Register(() =>
                {
                    taskCompletionSourceOfFileUnit?.TrySetCanceled();
                    Clear();
                });

                // 写入文件请求
                await channel.WriteAndFlushProtobufAsync(message);

                if (taskCompletionSourceOfFileUnit.Task.IsCanceled)
                    return null;
                else
                    return taskCompletionSourceOfFileUnit.Task.Result;
            }
        }
        catch (Exception ex)
        {
            Clear();
            throw;
        }
    }

    /// <summary>
    /// 文件上传完成回调
    /// </summary>
    /// <param name="response"></param>
    public void OnFileDownloadFinished(FileUnit? fileUnit)
    {
        if (taskCompletionSourceOfFileUnit == null || taskCompletionSourceOfFileUnit.Task.IsCompleted)
            return;

        taskCompletionSourceOfFileUnit.SetResult(fileUnit);
    }

    /// <summary>
    /// 接收到文件头回调
    /// </summary>
    /// <param name="fileHeader"></param>
    public async void OnFileHeaderReceived(FileHeader fileHeader)
    {
        var result = _regularFileOperator?.ReceiveFileHeader(fileHeader);

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
        var result = _regularFileOperator?.ReceiveFilePack(filePack);

        FilePackResponse response = new FilePackResponse
        {
            Success = result ?? false,
            FileName = filePack.FileName,
            PackIndex = filePack.PackIndex,
            Time = filePack.Time
        };
        // 发送文件分片响应
        if (_channel.TryGetTarget(out var channel))
            await channel.WriteAndFlushProtobufAsync(response);
    }

    #endregion

    /// <summary>
    /// 将连接对象返回连接池
    /// </summary>
    /// <returns></returns>
    public IChannel? ReturnChannel()
    {
        if (_channel.TryGetTarget(out var channel))
        {
            channel.Pipeline.Remove<RegularFileDownloadServerHandler>();
            return channel;
        }

        return null;
    }

    public void Clear()
    {
        // 取消订阅事件
        if (_regularFileOperator != null)
        {
            _regularFileOperator.OnFileDownloadFinished -= OnFileDownloadFinished;
            _regularFileOperator.Clear();
            _regularFileOperator.Dispose();
            _regularFileOperator = null;
        }

        // 清理未完成的任务
        if (taskCompletionSourceOfFileUnit != null && !taskCompletionSourceOfFileUnit.Task.IsCompleted)
        {
            taskCompletionSourceOfFileUnit.TrySetCanceled();
        }

        taskCompletionSourceOfFileUnit = null;
    }
}