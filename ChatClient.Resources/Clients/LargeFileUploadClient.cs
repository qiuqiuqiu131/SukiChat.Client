using System.Buffers;
using ChatClient.Resources.ServerHandlers;
using ChatServer.Common;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.Clients;

public class LargeFileUploadClient : IFileClient
{
    private readonly WeakReference<IChannel> _channel;
    private TaskCompletionSource<FileResponse>? taskCompletionSourceOfFileResponse;

    private string? _fileName;

    private IProgress<double>? _fileProgress;

    // 文件流
    private FileStream? _fileStream;

    // 文件操作
    const int CHUNK_SIZE = ushort.MaxValue; // 1MB per chunk
    byte[]? buffer;
    int _packIndex = 0;
    int _totelCount = 0;

    public LargeFileUploadClient(IChannel channel)
    {
        _channel = new WeakReference<IChannel>(channel);
        channel.Pipeline.AddLast(new LargeFileUploadServerHandler(this));
    }

    /// <summary>
    /// 上传大文件
    /// </summary>
    /// <param name="targetPath">服务器目标地址</param>
    /// <param name="fileName">文件名</param>
    /// <param name="file">大文件地址</param>
    /// <exception cref="NullReferenceException"></exception>
    public async Task UploadFile(string targetPath, string fileName, string filePath,
        IProgress<double> fileProgress)
    {
        if (!_channel.TryGetTarget(out IChannel? channel))
            throw new NullReferenceException();

        if (!channel.Active)
            throw new Exception("连接未激活，请检查连接状态");

        // 打开文件
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileInfo = new FileInfo(filePath);

        _fileName = fileName;
        _fileProgress = fileProgress;

        // 初始化本地文件读取
        buffer = new byte[CHUNK_SIZE];
        _packIndex = 0;
        _totelCount = (int)Math.Ceiling((double)fileInfo.Length / CHUNK_SIZE);

        // 等待服务器响应
        taskCompletionSourceOfFileResponse = new TaskCompletionSource<FileResponse>();

        // 发送文件头信息
        // 对方接受到头文件后会返回一个FilePackResponse，即可开始发送文件内容了
        FileHeader fileHeader = new FileHeader
        {
            Path = targetPath,
            FileName = fileName,
            Type = Path.GetExtension(fileName),
            TotleSize = (int)fileInfo.Length,
            TotleCount = _totelCount,
            Time = DateTime.Now.ToString("yyyyMMddHHmmss")
        };
        await channel.WriteAndFlushProtobufAsync(fileHeader);

        await taskCompletionSourceOfFileResponse.Task;
    }

    /// <summary>
    /// 接受到文件分片响应
    /// </summary>
    /// <param name="response"></param>
    public async void OnFilePackResponseReceived(FilePackResponse response)
    {
        if (!response.FileName.Equals(_fileName!) || !response.Success || response.PackIndex != _packIndex)
        {
            OnFileUploadFinished(new FileResponse { Success = false });
            return;
        }

        // 上传完成，更新文件上传进度
        _fileProgress?.Report((double)_packIndex / _totelCount);

        if (response.PackIndex == _totelCount) return;

        if (_fileStream == null) return;

        // 使用内存池获取缓冲区
        byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(CHUNK_SIZE);
        try
        {
            // 读取文件流
            int bytesRead = await _fileStream.ReadAsync(sharedBuffer, 0, CHUNK_SIZE);

            // 发送文件分片
            if (_channel.TryGetTarget(out var channel))
            {
                FilePack filePack = new FilePack
                {
                    PackIndex = ++_packIndex,
                    FileName = response.FileName,
                    PackSize = bytesRead,
                    Data = ByteString.CopyFrom(sharedBuffer, 0, bytesRead),
                    Time = response.Time
                };
                await channel.WriteAndFlushProtobufAsync(filePack);
            }
            else
            {
                OnFileUploadFinished(new FileResponse { Success = false });
            }
        }
        finally
        {
            // 归还缓冲区到池
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    /// <summary>
    /// 文件上传完成回调
    /// </summary>
    public void OnFileUploadFinished(FileResponse response)
    {
        if (!response.FileName.Equals(_fileName) || taskCompletionSourceOfFileResponse == null) return;
        taskCompletionSourceOfFileResponse.SetResult(response);

        if (buffer != null)
            Array.Clear(buffer);
        _fileStream?.Dispose();
        _fileStream = null;
    }

    public IChannel? ReturnChannel()
    {
        if (_channel.TryGetTarget(out var channel))
        {
            channel.Pipeline.Remove<LargeFileUploadServerHandler>();
            return channel;
        }

        return null;
    }

    // 修改Clear方法
    public void Clear()
    {
        if (_fileStream != null)
        {
            _fileStream.Dispose();
            _fileStream = null;
        }

        _fileProgress = null;
        _fileName = null;
        buffer = null;

        // 确保任务完成源设置为null
        taskCompletionSourceOfFileResponse = null;
    }
}