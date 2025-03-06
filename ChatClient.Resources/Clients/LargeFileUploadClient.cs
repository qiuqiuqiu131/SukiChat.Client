using ChatClient.ResourcesClient.ServerHandlers;
using ChatClient.Tool.Data.File;
using ChatServer.Common;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.Clients;

public class LargeFileUploadClient : IFileClient
{
    private readonly WeakReference<IChannel> _channel;
    private TaskCompletionSource<FileResponse>? taskCompletionSourceOfFileResponse;

    private FileProcessDto? _fileProcessDto;

    private string? _fileName;

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
        FileProcessDto? fileProcessDto = null)
    {
        if (!_channel.TryGetTarget(out IChannel? channel))
            throw new NullReferenceException();

        // 存储文件下载进度
        _fileProcessDto = fileProcessDto;

        // 打开文件
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileInfo = new FileInfo(filePath);

        _fileName = fileName;

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

    public void Clear()
    {
        if (_fileStream != null)
            _fileStream.Dispose();
        _fileStream = null;

        _fileProcessDto = null;

        if (buffer != null)
            Array.Clear(buffer);
    }
}