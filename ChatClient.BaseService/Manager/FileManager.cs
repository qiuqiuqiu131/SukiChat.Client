using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using File.Protobuf;

namespace ChatClient.BaseService.Manager;

public interface IFileManager
{
    Task OperateFileMessDto(string id, string userId, FileMessDto fileMessDto, FileTarget fileTarget);
}

public class FileManager : IFileManager, IDisposable
{
    private readonly IContainerProvider _containerProvider;
    private readonly IMessageHelper _messageHelper;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public FileManager(IContainerProvider containerProvider, IMessageHelper messageHelper)
    {
        _containerProvider = containerProvider;
        _messageHelper = messageHelper;
    }

    public async Task OperateFileMessDto(string id, string userId, FileMessDto fileMessDto,
        FileTarget fileTarget)
    {
        string path;
        if (fileTarget == FileTarget.User)
            path = Path.Combine("Users", id, "ChatFile");
        else
            path = Path.Combine("Groups", id, "ChatFile");

        await _semaphoreSlim.WaitAsync();
        try
        {
            var fileRequest = new FileRequest { FileName = fileMessDto.FileName, Path = path };
            var fileResponse = await _messageHelper.SendMessageWithResponse<FileHeader>(fileRequest);
            if (fileResponse?.Exist ?? false)
                fileMessDto.IsExit = true;
        }
        catch (Exception e)
        {
            // ignored
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        using (var scope = _containerProvider.CreateScope())
        {
            var _unitOfWork = scope.Resolve<IUnitOfWork>();
            // 查询数据库中文件状态
            if (fileTarget == FileTarget.User)
            {
                var chatPrivateFile = await _unitOfWork.GetRepository<ChatPrivateFile>()
                    .GetFirstOrDefaultAsync(predicate: d =>
                        d.ChatId.Equals(fileMessDto.ChatId) && d.UserId.Equals(userId));
                if (chatPrivateFile != null)
                {
                    fileMessDto.TargetFilePath = chatPrivateFile.TargetPath;
                    var fileInfo = new FileInfo(fileMessDto.TargetFilePath);
                    if (fileInfo.Exists && fileInfo.Length.Equals(fileMessDto.FileSize))
                        fileMessDto.IsSuccess = true;
                }
            }
            else if (fileTarget == FileTarget.Group)
            {
                var chatGroupFile = await _unitOfWork.GetRepository<ChatGroupFile>()
                    .GetFirstOrDefaultAsync(predicate: d =>
                        d.ChatId.Equals(fileMessDto.ChatId) && d.UserId.Equals(userId));
                if (chatGroupFile != null)
                {
                    fileMessDto.TargetFilePath = chatGroupFile.TargetPath;
                    var fileInfo = new FileInfo(fileMessDto.TargetFilePath);
                    if (fileInfo.Exists && fileInfo.Length.Equals(fileMessDto.FileSize))
                        fileMessDto.IsSuccess = true;
                }
            }
        }
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }
}