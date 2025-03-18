using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IChatService
{
    Task<(bool, string)> SendChatMessage(string userId, string targetId, List<ChatMessageDto> messages);
    Task<(bool, string)> SendGroupChatMessage(string userId, string groupId, List<ChatMessageDto> messages);
    Task OperateChatMessage(string id, int chatId, List<ChatMessageDto> chatMessages, FileTarget fileTarget);

    Task SendFriendWritingMessage(string? userId, string? targetId, bool isWriting);
    Task UpdateFileMess(FileMessDto messages);
    Task<bool> ReadAllChatMessage(string userId, string targetId, int chatId, FileTarget fileTarget);
}

internal class ChatService : BaseService, IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageHelper _messageHelper;
    private readonly IImageManager _imageManager;

    public ChatService(IContainerProvider containerProvider,
        IMapper mapper,
        IMessageHelper messageHelper,
        IImageManager imageManager) : base(
        containerProvider)
    {
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
        _mapper = mapper;
        _messageHelper = messageHelper;
        _imageManager = imageManager;
    }

    #region SendMessage

    /// <summary>
    /// 发送私聊消息，如果发送图片消息，会上传图片，再发送消息
    /// </summary>
    /// <param name="userId">当前用户Id</param>
    /// <param name="targetId">目标用户Id</param>
    /// <param name="messages">消息体（文字表情包，或者单张图片）</param>
    /// <returns></returns>
    public async Task<(bool, string)> SendChatMessage(string userId, string targetId, List<ChatMessageDto> messages)
    {
        var chatMessages = new List<ChatMessage>();
        foreach (var mess in messages)
        {
            var chatMessage = _mapper.Map<ChatMessage>(mess);
            // 如果是图片消息，上传图片,ChatMessage.Content为图片路径
            if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.ImageMess)
            {
                var (state, message) = await UploadChatImage(userId, ((ImageMessDto)mess.Content).ImageSource,
                    FileTarget.User, ((ImageMessDto)mess.Content).FilePath);

                if (!state) return (false, message);
                chatMessage.ImageMess.FilePath = message;
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.FileMess)
            {
                var fileMess = (FileMessDto)mess.Content;
                var (state, message) = await UploadChatFile(userId, chatMessage.FileMess.FileName, FileTarget.User,
                    fileMess.FileProcessDto);

                // 上传完成，取消下载状态
                fileMess.FileProcessDto = null;

                if (!state) return (false, message);
                chatMessage.FileMess.FileName = message;
            }

            chatMessages.Add(chatMessage);
        }

        // 构建消息体
        var friendMessage = new FriendChatMessage
        {
            UserFromId = userId,
            UserTargetId = targetId,
        };
        friendMessage.Messages.AddRange(chatMessages);

        var response = await _messageHelper.SendMessageWithResponse<FriendChatMessageResponse>(friendMessage);

        //-- 判断: 是否发送成功 --//
        if (!(response is { Response: { State: true } }))
            return (false, response?.Response?.Message ?? "Failed to send message");

        //-- 操作: 将消息存入数据库 --//
        friendMessage.Id = response.Id;
        friendMessage.Time = response.Time;
        var chatPrivate = _mapper.Map<ChatPrivate>(friendMessage);
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        try
        {
            var result =
                await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(response.Id));
            if (result != null)
                chatPrivate.Id = result.Id;
            chatPrivateRepository.Update(chatPrivate);
            await _unitOfWork.SaveChangesAsync();
            if (result != null)
                chatPrivateRepository.ChangeEntityState(result, EntityState.Detached);
        }
        catch (Exception e)
        {
            // ignored
        }

        return (true, response.Id.ToString());
    }

    /// <summary>
    /// 发送群聊消息，如果发送图片消息，会上传图片，再发送消息
    /// </summary>
    /// <param name="userId">当前用户Id</param>
    /// <param name="groupId">目标群聊Id</param>
    /// <param name="messages">消息体（文字表情包，或者单张图片）</param>
    /// <returns></returns>
    public async Task<(bool, string)> SendGroupChatMessage(string userId, string groupId, List<ChatMessageDto> messages)
    {
        var chatMessages = new List<ChatMessage>();
        foreach (var mess in messages)
        {
            var chatMessage = _mapper.Map<ChatMessage>(mess);
            // 如果是图片消息，上传图片,ChatMessage.Content为图片路径
            if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.ImageMess)
            {
                var (state, message) = await UploadChatImage(groupId, ((ImageMessDto)mess.Content).ImageSource,
                    FileTarget.Group, ((ImageMessDto)mess.Content).FilePath);

                if (!state) return (false, message);
                chatMessage.ImageMess.FilePath = message;
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.FileMess)
            {
                var fileMess = (FileMessDto)mess.Content;
                var (state, message) = await UploadChatFile(groupId, chatMessage.FileMess.FileName, FileTarget.Group,
                    fileMess.FileProcessDto);

                // 上传完成，取消下载状态
                fileMess.FileProcessDto = null;

                if (!state) return (false, message);
                chatMessage.FileMess.FileName = message;
            }

            chatMessages.Add(chatMessage);
        }

        var groupChatMessage = new GroupChatMessage
        {
            UserFromId = userId,
            GroupId = groupId,
        };
        groupChatMessage.Messages.AddRange(chatMessages);

        var response = await _messageHelper.SendMessageWithResponse<GroupChatMessageResponse>(groupChatMessage);
        //-- 判断: 是否发送成功 --//
        if (!(response is { Response: { State: true } }))
            return (false, response?.Response?.Message ?? "Failed to send message");

        //-- 操作: 将消息存入数据库 --//
        groupChatMessage.Id = response.Id;
        groupChatMessage.Time = response.Time;
        var chatGroup = _mapper.Map<ChatGroup>(groupChatMessage);
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        try
        {
            var result = await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(response.Id));
            if (result != null)
                chatGroup.Id = result.Id;
            chatGroupRepository.Update(chatGroup);
            await _unitOfWork.SaveChangesAsync();
            if (result != null)
                chatGroupRepository.ChangeEntityState(result, EntityState.Detached);
        }
        catch (Exception e)
        {
            // ignored
        }

        return (true, response.Id.ToString());
    }

    /// <summary>
    /// 发送用户正在输入的消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <param name="isWriting"></param>
    public async Task SendFriendWritingMessage(string? userId, string? targetId, bool isWriting)
    {
        if (userId == null || targetId == null) return;

        var friendWritingMessage = new FriendWritingMessage
        {
            UserFromId = userId,
            UserTargetId = targetId,
            IsWriting = isWriting
        };
        await _messageHelper.SendMessage(friendWritingMessage);
    }

    #endregion

    #region OperateFriendResources

    /// <summary>
    /// 上传聊天图片，不会直接调用，而是在发送消息时调用
    /// 如果上传成功，返回true和文件名，否则返回false和错误信息
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="bitmap"></param>
    /// <param name="fileTarget"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    private async Task<(bool, string)> UploadChatImage(string Id, Bitmap bitmap, FileTarget fileTarget,
        string? filename = null)
    {
        var _fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();

        var fileName =
            $"{DateTime.Now:yyyyMMddHHmmss}_{(string.IsNullOrWhiteSpace(filename) ? "图片" : Path.GetFileName(filename))}.png";
        var result =
            await _fileOperateHelper.UploadFile(Id, "ChatFile", fileName, bitmap.BitmapToByteArray(),
                fileTarget);

        if (result)
            return (true, fileName);
        return (false, "Failed to upload image");
    }

    /// <summary>
    /// 上传聊天文件，不会直接调用，而是在发送消息时调用
    /// 如果上传成功，返回true和文件名，否则返回false和错误信息
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="filePath"></param>
    /// <param name="fileTarget"></param>
    /// <param name="fileProcessDto"></param>
    /// <returns></returns>
    private async Task<(bool, string)> UploadChatFile(string Id, string filePath, FileTarget fileTarget,
        FileProcessDto? fileProcessDto = null)
    {
        if (!System.IO.File.Exists(filePath)) return (false, "File not found");

        var _fileIOHelper = _scopedProvider.Resolve<IFileIOHelper>();

        var basePath = fileTarget switch
        {
            FileTarget.Group => "Groups",
            FileTarget.User => "Users",
        };
        var path = Path.Combine(basePath, Id, "ChatFile");

        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(filePath)}";
        var result = await _fileIOHelper.UploadLargeFileAsync(path, fileName, filePath, fileProcessDto);

        if (result)
            return (true, fileName);
        return (false, "Failed to upload image");
    }

    #endregion

    /// <summary>
    /// 用于更新文件下载状态,数据库中更改
    /// </summary>
    /// <param name="fileMess"></param>
    public async Task UpdateFileMess(FileMessDto fileMess)
    {
        int chatId = fileMess.ChatId;
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var chatPrivate = await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.ChatId.Equals(chatId));

        if (chatPrivate == null) return;

        // 拼接消息
        List<ChatMessageDto> messages =
        [
            new ChatMessageDto
            {
                Type = ChatMessage.ContentOneofCase.FileMess,
                Content = fileMess
            }
        ];

        string mess = ChatMessageTool.EncruptChatMessageDto(messages);
        chatPrivate.Message = mess;

        chatPrivateRepository.Update(chatPrivate);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// 将所有的消息设置为已读
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ReadAllChatMessage(string userId, string targetId, int lastChatId, FileTarget fileTarget)
    {
        // TODO: 已读所有消息
        if (fileTarget == FileTarget.User)
        {
            var request = new UpdateFriendLastChatIdRequest
            {
                UserId = userId,
                FriendId = targetId,
                LastChatId = lastChatId
            };
            var response = await _messageHelper.SendMessageWithResponse<UpdateFriendLastChatIdResponse>(request);
            if (response is { Response: { State: true } })
            {
                var repository = _unitOfWork.GetRepository<FriendRelation>();
                var relation = await repository.GetFirstOrDefaultAsync(
                    predicate: d => d.User1Id.Equals(userId) && d.User2Id.Equals(targetId), disableTracking: false);
                if (relation != null)
                {
                    if (relation.LastChatId < lastChatId)
                    {
                        relation.LastChatId = lastChatId;
                        await _unitOfWork.SaveChangesAsync();
                    }

                    return true;
                }
            }

            return false;
        }
        else if (fileTarget == FileTarget.Group)
        {
            var request = new UpdateGroupLastChatIdRequest
            {
                UserId = userId,
                GroupId = targetId,
                LastChatId = lastChatId
            };
            var response = await _messageHelper.SendMessageWithResponse<UpdateGroupLastChatIdResponse>(request);
            if (response is { Response: { State: true } })
            {
                var repository = _unitOfWork.GetRepository<GroupRelation>();
                var relation = await repository.GetFirstOrDefaultAsync(
                    predicate: d => d.UserId.Equals(userId) && d.GroupId.Equals(targetId), disableTracking: false);
                if (relation != null)
                {
                    if (relation.LastChatId < lastChatId)
                    {
                        relation.LastChatId = lastChatId;
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// 将ChatMessages中的资源注入
    /// </summary>
    /// <param name="id"></param>
    /// <param name="chatId"></param>
    /// <param name="chatMessages"></param>
    /// <param name="fileTarget"></param>
    public async Task OperateChatMessage(string id,
        int chatId,
        List<ChatMessageDto> chatMessages, FileTarget fileTarget)
    {
        foreach (var chatMessage in chatMessages)
        {
            if (chatMessage.Type == ChatMessage.ContentOneofCase.ImageMess)
            {
                var messContent = (ImageMessDto)chatMessage.Content;
                string filename = messContent.FilePath;
                var content = await _imageManager.GetFile(
                    id, "ChatFile", filename, fileTarget);
                if (content != null)
                    messContent.ImageSource = content;
                else
                {
                    //TODO:图片失效处理
                }
            }
            else if (chatMessage.Type == ChatMessage.ContentOneofCase.FileMess)
            {
                var messContent = (FileMessDto)chatMessage.Content;
                messContent.ChatId = chatId;
                if (string.IsNullOrWhiteSpace(messContent.TargetFilePath)) return;
                FileInfo fileInfo = new FileInfo(messContent.TargetFilePath);
                if (fileInfo.Exists && fileInfo.Length == messContent.FileSize)
                    messContent.IsDownload = true;
                else
                {
                    if (fileInfo.Exists)
                    {
                        FileProcessDto fileProcessDto = new FileProcessDto
                        {
                            FileName = messContent.FileName,
                            CurrentSize = fileInfo.Length,
                            MaxSize = messContent.FileSize
                        };
                        messContent.FileProcessDto = fileProcessDto;
                    }
                }
            }
        }
    }
}