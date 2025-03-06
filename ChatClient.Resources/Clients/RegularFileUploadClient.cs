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

    private FileProcessDto? _fileProcessDto;

    private string? _fileName;

    private MemoryStream? _fileStream;

    // 文件操作
    const int CHUNK_SIZE = 60000; // 64KB per chunk
    byte[]? buffer;
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
    public async Task<FileResponse?> UploadFile(string targetPath, string fileName, byte[] file,
        FileProcessDto? fileProcessDto = null)
    {
        if (!_channel.TryGetTarget(out IChannel? channel))
            throw new NullReferenceException();

        // 存储文件下载进度
        _fileProcessDto = fileProcessDto;

        // 生成Stream流
        _fileStream = new MemoryStream(file, writable: false);

        _fileName = fileName;

        // 发送文件头信息
        // 对方接受到头文件后会返回一个FilePackResponse，即可开始发送文件内容了
        FileHeader fileHeader = new FileHeader
        {
            Path = targetPath,
            FileName = fileName,
            Type = Path.GetExtension(fileName),
            TotleSize = file.Length,
            TotleCount = (int)Math.Ceiling((double)file.Length / CHUNK_SIZE),
            Time = DateTime.Now.ToString("yyyyMMddHHmmss")
        };
        await channel.WriteAndFlushProtobufAsync(fileHeader);

        // 初始化文件资源读取
        buffer = new byte[CHUNK_SIZE];
        _packIndex = 0;
        _totelCount = fileHeader.TotleCount;

        taskCompletionSourceOfFileResponse = new TaskCompletionSource<FileResponse>();
        var result = await taskCompletionSourceOfFileResponse.Task;
        taskCompletionSourceOfFileResponse = null;
        return result;
    }

    public async void OnFilePackResponseReceived(FilePackResponse response)
    {
        if (!response.FileName.Equals(_fileName!) || !response.Success ||
            response.PackIndex != _packIndex)
        {
            OnFileUploadFinished(new FileResponse { Success = false });
            return;
        }

        // 更新上传进度
        if (_fileProcessDto != null)
            _fileProcessDto.CurrentSize += response.PackSize;

        if (response.PackIndex == _totelCount) return;

        if (_fileStream == null) return;

        // 读取文件流
        int bytesRead = await _fileStream.ReadAsync(buffer!, 0, CHUNK_SIZE);

        // 拷贝读取的字节流
        byte[] chunk = new byte[bytesRead];
        Array.Copy(buffer!, chunk, bytesRead);

        // 发送文件分片
        if (_channel.TryGetTarget(out var channel))
        {
            FilePack filePack = new FilePack
            {
                PackIndex = ++_packIndex,
                FileName = response.FileName,
                PackSize = bytesRead,
                Data = ByteString.CopyFrom(chunk),
                Time = response.Time
            };
            await channel.WriteAndFlushProtobufAsync(filePack);
        }
        else
        {
            OnFileUploadFinished(new FileResponse { Success = false });
        }

        Array.Clear(chunk);
    }

    public void OnFileUploadFinished(FileResponse response)
    {
        if (!response.FileName.Equals(_fileName) || taskCompletionSourceOfFileResponse == null) return;
        taskCompletionSourceOfFileResponse.SetResult(response);

        // 清理资源
        if (buffer != null)
            Array.Clear(buffer);
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
            _fileStream.Dispose();

        _fileProcessDto = null;

        if (buffer != null)
            Array.Clear(buffer);
    }
}