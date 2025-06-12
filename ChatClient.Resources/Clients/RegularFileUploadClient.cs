using System.Buffers;
using ChatClient.Resources.ServerHandlers;
using ChatClient.Tool.Data.File;
using ChatServer.Common;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.Clients;

public class RegularFileUploadClient : IFileClient
{
    private readonly WeakReference<IChannel> _channel;
    private TaskCompletionSource<FileResponse>? taskCompletionSourceOfFileResponse;

    private string? _fileName;

    public MemoryStream? _fileStream;

    // 文件操作
    const int CHUNK_SIZE = 60000; // 64KB per chunk
    int _packIndex = 0;
    int _totelCount = 0;

    public RegularFileUploadClient(IChannel channel)
    {
        _channel = new WeakReference<IChannel>(channel);
        channel.Pipeline.AddLast(new RegularFileUploadServerHandler(this));
    }

    /// <summary>
    /// 上传文件
    /// 请使用try-catch捕获异常
    /// 1、连接超时会抛出ConnectTimeoutException
    /// 2、连接对象未初始化会抛出NullReferenceException
    /// </summary>
    public async Task<FileResponse?> UploadFile(string targetPath, string fileName, byte[] file)
    {
        if (!_channel.TryGetTarget(out IChannel? channel))
            throw new NullReferenceException();
        
        if(!channel.Active)
            throw new Exception("连接未激活，请检查连接状态");

        // 生成Stream流
        _fileStream = new MemoryStream(file);

        _fileName = fileName;

        // 初始化文件资源读取
        _packIndex = 0;
        _totelCount = (int)Math.Ceiling((double)file.Length / CHUNK_SIZE);

        taskCompletionSourceOfFileResponse = new TaskCompletionSource<FileResponse>();

        // 发送文件头信息
        // 对方接受到头文件后会返回一个FilePackResponse，即可开始发送文件内容了
        FileHeader fileHeader = new FileHeader
        {
            Path = targetPath,
            FileName = fileName,
            Type = Path.GetExtension(fileName),
            TotleSize = file.Length,
            TotleCount = _totelCount,
            Time = DateTime.Now.ToString("yyyyMMddHHmmss")
        };
        await channel.WriteAndFlushProtobufAsync(fileHeader);

        await taskCompletionSourceOfFileResponse.Task;
        return taskCompletionSourceOfFileResponse.Task.Result;
    }

    /// <summary>
    /// 对方接收到文件包后回复
    /// </summary>
    /// <param name="response"></param>
    public async void OnFilePackResponseReceived(FilePackResponse response)
    {
        if (!response.FileName.Equals(_fileName!) || !response.Success ||
            response.PackIndex != _packIndex)
        {
            OnFileUploadFinished(new FileResponse { Success = false });
            Console.WriteLine("服务器文件接受失败");
            return;
        }

        if (response.PackIndex == _totelCount) return;

        if (_fileStream == null) return;

        // 使用共享对象池获取缓冲区
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
            // 归还缓冲区
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    public void OnFileUploadFinished(FileResponse response)
    {
        // Console.WriteLine($"FileUploadFinish:{response.Success},{response.FileName}");
        if (!response.FileName.Equals(_fileName) || taskCompletionSourceOfFileResponse == null) return;
        taskCompletionSourceOfFileResponse.SetResult(response);

        // 清理资源
        _fileStream?.Dispose();
        _fileStream = null;
    }

    public IChannel? ReturnChannel()
    {
        if (_channel.TryGetTarget(out var channel))
        {
            channel.Pipeline.Remove<RegularFileUploadServerHandler>();
            return channel;
        }

        return null;
    }

    public void Clear()
    {
        if (_fileStream != null)
        {
            _fileStream.Dispose();
            _fileStream = null;
        }

        _fileName = null;
    }
}