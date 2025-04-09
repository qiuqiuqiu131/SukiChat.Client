using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Media.Audio;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
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

    Task OperateChatMessage(string userId, string id, int chatId, bool isUser, List<ChatMessageDto> chatMessages,
        FileTarget fileTarget);

    Task SendFriendWritingMessage(string? userId, string? targetId, bool isWriting);
    Task UpdateFileMess(string userId, FileMessDto fileMess, FileTarget target);
    Task<bool> ReadAllChatMessage(string userId, string targetId, int chatId, FileTarget fileTarget);
    Task AddChatDto(object obj);

    Task<bool> DeleteChatMessage(string userId, int chatId, FileTarget fileTarget);
    Task<bool> RetractChatMessage(string userId, int chatId, FileTarget fileTarget);

    Task<bool> SendChatShareMessage(string userId, ChatMessageDto chatMessageDto, string senderMessage,
        IEnumerable<object> relations);
}

internal class ChatService : BaseService, IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAppDataManager _appDataManager;
    private readonly IMessageHelper _messageHelper;
    private readonly IFileOperateHelper _fileOperateHelper;
    private readonly IImageManager _imageManager;

    public ChatService(IContainerProvider containerProvider,
        IMapper mapper,
        IAppDataManager appDataManager,
        IMessageHelper messageHelper,
        IFileOperateHelper fileOperateHelper,
        IImageManager imageManager) : base(
        containerProvider)
    {
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
        _mapper = mapper;
        _appDataManager = appDataManager;
        _messageHelper = messageHelper;
        _fileOperateHelper = fileOperateHelper;
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
                if (mess.Content is ImageMessDto imageMessDto)
                    imageMessDto.ActualPath =
                        _appDataManager.GetFilePath(Path.Combine("Users", userId, "ChatFile", message));
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.VoiceMess)
            {
                var (state, message) = await UploadChatVoice(userId, ((VoiceMessDto)mess.Content).AudioData,
                    FileTarget.User, ((VoiceMessDto)mess.Content).FilePath);

                // 清空AudioData
                if (!state) return (false, message);
                chatMessage.VoiceMess.FilePath = message;
                if (mess.Content is VoiceMessDto voiceMessDto)
                    voiceMessDto.ActualPath =
                        _appDataManager.GetFilePath(Path.Combine("Users", userId, "ChatFile", message));
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.FileMess)
            {
                var fileMess = (FileMessDto)mess.Content;
                fileMess.IsDownloading = true;
                var progress = new Progress<double>(d => fileMess.DownloadProgress = d);
                var (state, message) =
                    await UploadChatFile(userId, chatMessage.FileMess.FileName, FileTarget.User, progress);

                // 上传完成，取消下载状态
                fileMess.IsDownloading = false;
                fileMess.IsSuccess = state;

                if (!state) return (false, message);
                chatMessage.FileMess.FileName = message;
                if (mess.Content is FileMessDto fileMessDto)
                    fileMessDto.FileName = message;
            }

            chatMessages.Add(chatMessage);
        }

        // 构建消息体
        var friendMessage = new FriendChatMessage
        {
            UserFromId = userId,
            UserTargetId = targetId
        };
        friendMessage.Messages.AddRange(chatMessages);

        var response = await _messageHelper.SendMessageWithResponse<FriendChatMessageResponse>(friendMessage);

        //-- 判断: 是否发送成功 --//
        if (!(response is { Response: { State: true } }))
            return (false, response?.Response?.Message ?? "Failed to send message");

        foreach (var message in messages)
            if (message.Content is FileMessDto fileMessDto)
                fileMessDto.ChatId = response.Id;

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
                if (mess.Content is ImageMessDto imageMessDto)
                    imageMessDto.ActualPath =
                        _appDataManager.GetFilePath(Path.Combine("Groups", groupId, "ChatFile", message));
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.VoiceMess)
            {
                var (state, message) = await UploadChatVoice(groupId, ((VoiceMessDto)mess.Content).AudioData,
                    FileTarget.Group, ((VoiceMessDto)mess.Content).FilePath);

                // 清空AudioData
                if (!state) return (false, message);
                chatMessage.VoiceMess.FilePath = message;
                if (mess.Content is VoiceMessDto voiceMessDto)
                    voiceMessDto.ActualPath =
                        _appDataManager.GetFilePath(Path.Combine("Groups", groupId, "ChatFile", message));
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.FileMess)
            {
                var fileMess = (FileMessDto)mess.Content;
                fileMess.IsDownloading = true;
                var progress = new Progress<double>(d => fileMess.DownloadProgress = d);
                var (state, message) =
                    await UploadChatFile(groupId, chatMessage.FileMess.FileName, FileTarget.Group, progress);

                fileMess.IsDownloading = false;
                fileMess.IsSuccess = state;

                if (!state) return (false, message);
                chatMessage.FileMess.FileName = message;
            }

            chatMessages.Add(chatMessage);
        }

        var groupChatMessage = new GroupChatMessage
        {
            UserFromId = userId,
            GroupId = groupId
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
    /// 上传聊天语音，不会直接调用，而是在发送消息时调用
    /// </summary>
    /// <param name="id"></param>
    /// <param name="voice"></param>
    /// <param name="fileTarget"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    private async Task<(bool, string)> UploadChatVoice(string id, byte[] voice, FileTarget fileTarget,
        string? filename = null)
    {
        var _fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();

        var fileName =
            $"{DateTime.Now:yyyyMMddHHmmss}_{(string.IsNullOrWhiteSpace(filename) ? "语音" : Path.GetFileName(filename))}.mp3";
        var result =
            await _fileOperateHelper.UploadFile(id, "ChatFile", fileName, voice,
                fileTarget);

        if (result)
            return (true, fileName);
        return (false, "Failed to upload voice");
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
        IProgress<double> fileProgress)
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
        var result = await _fileIOHelper.UploadLargeFileAsync(path, fileName, filePath, fileProgress);

        if (result)
            return (true, fileName);
        return (false, "Failed to upload image");
    }

    #endregion

    /// <summary>
    /// 用于更新文件下载状态,数据库中更改
    /// </summary>
    /// <param name="fileMess"></param>
    public async Task UpdateFileMess(string userId, FileMessDto fileMess, FileTarget target)
    {
        if (target == FileTarget.User)
        {
            var chatPrivateFile = new ChatPrivateFile
            {
                ChatId = fileMess.ChatId,
                TargetPath = fileMess.TargetFilePath,
                UserId = userId
            };
            var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivateFile>();
            var chatPrivate = await chatPrivateRepository.GetFirstOrDefaultAsync(
                predicate: d => d.ChatId.Equals(fileMess.ChatId) && d.UserId.Equals(userId), disableTracking: false);
            if (chatPrivate != null)
                chatPrivate.TargetPath = fileMess.TargetFilePath;
            else
                await chatPrivateRepository.InsertAsync(chatPrivateFile);
        }
        else
        {
            var chatGroupFile = new ChatGroupFile
            {
                ChatId = fileMess.ChatId,
                TargetPath = fileMess.TargetFilePath,
                UserId = userId
            };
            var chatGroupRepository = _unitOfWork.GetRepository<ChatGroupFile>();
            var chatGroup = await chatGroupRepository.GetFirstOrDefaultAsync(
                predicate: d => d.ChatId.Equals(fileMess.ChatId) && d.UserId.Equals(userId), disableTracking: false);
            if (chatGroup != null)
                chatGroup.TargetPath = fileMess.TargetFilePath;
            else
                await chatGroupRepository.InsertAsync(chatGroupFile);
        }

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

                    var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
                    var friendRelation = await userDtoManager.GetFriendRelationDto(userId, targetId);
                    if (friendRelation != null)
                        friendRelation.LastChatId = lastChatId;

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

                var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
                var groupRelation = await userDtoManager.GetGroupRelationDto(userId, targetId);
                if (groupRelation != null)
                    groupRelation.LastChatId = lastChatId;

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
    public async Task OperateChatMessage(string userId, string id,
        int chatId, bool isUser,
        List<ChatMessageDto> chatMessages, FileTarget fileTarget)
    {
        foreach (var chatMessage in chatMessages)
        {
            if (chatMessage.Type == ChatMessage.ContentOneofCase.ImageMess)
            {
                var messContent = (ImageMessDto)chatMessage.Content;
                var basePath = fileTarget switch
                {
                    FileTarget.Group => "Groups",
                    FileTarget.User => "Users"
                };
                var actualPath = Path.Combine(basePath, isUser ? userId : id, "ChatFile", messContent.FilePath);
                messContent.ActualPath = _appDataManager.GetFileInfo(actualPath).FullName;

                // 获取文件
                string filename = messContent.FilePath;
                var content = await _imageManager.GetChatFile(
                    isUser ? userId : id, "ChatFile", filename, fileTarget);
                if (content != null)
                    messContent.ImageSource = content;
                else
                {
                    messContent.Failed = true;
                    messContent.ImageSource = null;
                }
            }
            else if (chatMessage.Type == ChatMessage.ContentOneofCase.VoiceMess)
            {
                var messContent = (VoiceMessDto)chatMessage.Content;
                var basePath = fileTarget switch
                {
                    FileTarget.Group => "Groups",
                    FileTarget.User => "Users"
                };
                var actualPath = Path.Combine(basePath, isUser ? userId : id, "ChatFile", messContent.FilePath);
                messContent.ActualPath = _appDataManager.GetFileInfo(actualPath).FullName;

                var bytes = await _fileOperateHelper.GetFile(isUser ? userId : id, "ChatFile", messContent.FilePath,
                    fileTarget);
                if (bytes == null)
                    messContent.Failed = true;
                else
                {
                    messContent.AudioData = bytes;
                    using (var audioPlayer = new AudioPlayer())
                    {
                        try
                        {
                            audioPlayer.LoadFromMemory(messContent.AudioData);
                            messContent.Duration = audioPlayer.TotalTime;
                        }
                        catch (Exception e)
                        {
                            messContent.Failed = true;
                            messContent.AudioData = null;
                        }
                    }
                }
            }
            else if (chatMessage.Type == ChatMessage.ContentOneofCase.FileMess)
            {
                var messContent = (FileMessDto)chatMessage.Content;
                messContent.ChatId = chatId;
                messContent.IsUser = isUser;

                var fileManager = _scopedProvider.Resolve<IFileManager>();
                await fileManager.OperateFileMessDto(id, userId, messContent, fileTarget);
            }
            else if (chatMessage.Type == ChatMessage.ContentOneofCase.CardMess)
            {
                var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
                if (chatMessage.Content is CardMessDto cardMessDto)
                {
                    cardMessDto.IsSelf = isUser;
                    if (cardMessDto.IsUser)
                        cardMessDto.Content = await userDtoManager.GetUserDto(cardMessDto.Id);
                    else
                        cardMessDto.Content = await userDtoManager.GetGroupDto(userId, cardMessDto.Id);
                }
            }
        }
    }

    public async Task AddChatDto(object obj)
    {
        var userManager = _scopedProvider.Resolve<IUserManager>();
        if (obj is FriendRelationDto friendRelationDto)
        {
            var dto = userManager.FriendChats!.FirstOrDefault(d => d.FriendRelatoinDto == friendRelationDto);
            if (dto != null) return;

            var friendChatPackService = _scopedProvider.Resolve<IFriendChatPackService>();
            var friendChatDto =
                await friendChatPackService.GetFriendChatDto(userManager.User!.Id, friendRelationDto.Id);
            userManager.FriendChats!.Add(friendChatDto);
            friendRelationDto.IsChatting = true;
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            var dto = userManager.GroupChats!.FirstOrDefault(d => d.GroupRelationDto == groupRelationDto);
            if (dto != null) return;

            var groupChatPackService = _scopedProvider.Resolve<IGroupChatPackService>();
            var groupChatDto = await groupChatPackService.GetGroupChatDto(userManager.User!.Id, groupRelationDto.Id);
            userManager.GroupChats!.Add(groupChatDto);
            groupRelationDto.IsChatting = true;
        }
    }

    public async Task<bool> DeleteChatMessage(string userId, int chatId, FileTarget fileTarget)
    {
        if (fileTarget == FileTarget.User)
        {
            var request = new ChatPrivateDeleteRequest
            {
                ChatPrivateId = chatId,
                UserId = userId
            };

            var result = await _messageHelper.SendMessageWithResponse<ChatPrivateDeleteResponse>(request);

            if (result?.Response?.State ?? false)
            {
                try
                {
                    var repository = _unitOfWork.GetRepository<ChatPrivateDetail>();
                    var entity = await repository.GetFirstOrDefaultAsync(predicate: d =>
                        d.UserId.Equals(userId) && d.ChatPrivateId.Equals(chatId), disableTracking: false);
                    if (entity != null)
                        entity.IsDeleted = true;
                    else
                    {
                        var entityDetail = new ChatPrivateDetail
                        {
                            ChatPrivateId = chatId,
                            UserId = userId,
                            IsDeleted = true
                        };
                        await repository.InsertAsync(entityDetail);
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignore
                }
            }

            return result?.Response?.State ?? false;
        }
        else if (fileTarget == FileTarget.Group)
        {
            var request = new ChatGroupDeleteRequest
            {
                ChatGroupId = chatId,
                UserId = userId
            };

            var result = await _messageHelper.SendMessageWithResponse<ChatGroupDeleteResponse>(request);

            if (result?.Response?.State ?? false)
            {
                try
                {
                    var repository = _unitOfWork.GetRepository<ChatGroupDetail>();
                    var entity = await repository.GetFirstOrDefaultAsync(predicate: d =>
                        d.UserId.Equals(userId) && d.ChatGroupId.Equals(chatId), disableTracking: false);
                    if (entity != null)
                        entity.IsDeleted = true;
                    else
                    {
                        var entityDetail = new ChatGroupDetail
                        {
                            ChatGroupId = chatId,
                            UserId = userId,
                            IsDeleted = true
                        };
                        await repository.InsertAsync(entityDetail);
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignore
                }
            }

            return result?.Response?.State ?? false;
        }

        return false;
    }

    public async Task<bool> RetractChatMessage(string userId, int chatId, FileTarget fileTarget)
    {
        if (fileTarget == FileTarget.User)
        {
            var request = new ChatPrivateRetractRequest
            {
                UserId = userId,
                ChatPrivateId = chatId
            };

            var result = await _messageHelper.SendMessageWithResponse<ChatPrivateRetractMessage>(request);
            return result?.Response?.State ?? false;
        }
        else if (fileTarget == FileTarget.Group)
        {
            var request = new ChatGroupRetractRequest
            {
                UserId = userId,
                ChatGroupId = chatId
            };

            var result = await _messageHelper.SendMessageWithResponse<ChatGroupRetractMessage>(request);
            return result?.Response?.State ?? false;
        }

        return false;
    }

    public async Task<bool> SendChatShareMessage(string userId, ChatMessageDto chatMessageDto, string senderMessage,
        IEnumerable<object> relations)
    {
        List<TargetInfo> targetInfos = [];
        foreach (var relation in relations)
        {
            if (relation is FriendRelationDto friendRelationDto)
                targetInfos.Add(new TargetInfo { Id = friendRelationDto.Id, IsUser = true });
            else if (relation is GroupRelationDto groupRelationDto)
                targetInfos.Add(new TargetInfo { Id = groupRelationDto.Id, IsUser = false });
        }

        var mess = _mapper.Map<ChatMessage>(chatMessageDto);

        var request = new ChatShareMessageRequest
        {
            Messages = _mapper.Map<ChatMessage>(chatMessageDto),
            SenderMessage = senderMessage,
            UserId = userId
        };
        request.TargetIds.AddRange(targetInfos);

        var result = await _messageHelper.SendMessageWithResponse<ChatShareMessageResponse>(request);
        return result?.Response?.State ?? false;
    }
}