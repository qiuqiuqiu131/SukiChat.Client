using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IChatService
{
    Task<(bool, string)> SendChatMessage(string userId, string targetId, List<ChatMessageDto> messages);
    Task<(bool, string)> SendGroupChatMessage(string userId, string groupId, List<ChatMessageDto> messages);
    Task<byte[]?> GetChatImage(string userId, string fileName);
    Task<byte[]?> GetCompressedChatImage(string userId, string fileName);
    Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage);
    Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages);
    Task SendFriendWritingMessage(string? userId, string? targetId, bool isWriting);
    Task UpdateFileMess(FileMessDto messages);
    Task ReadAllChatMessage(string userId, string targetId);
}

internal class ChatService : BaseService, IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageHelper _messageHelper;

    public ChatService(IContainerProvider containerProvider, IMapper mapper, IMessageHelper messageHelper) : base(
        containerProvider)
    {
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
        _mapper = mapper;
        _messageHelper = messageHelper;
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
                    ((ImageMessDto)mess.Content).FilePath);

                if (!state) return (false, message);
                chatMessage.ImageMess.FilePath = message;
            }
            else if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.FileMess)
            {
                var fileMess = (FileMessDto)mess.Content;
                var (state, message) = await UploadChatFile(userId, chatMessage.FileMess.FileName,
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
        var result = await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(response.Id));
        if (result != null)
            chatPrivate.Id = result.Id;
        chatPrivateRepository.Update(chatPrivate);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Message sent successfully");
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
        var groupChatMessages = _mapper.Map<List<ChatMessage>>(messages);
        foreach (var chatMessage in groupChatMessages)
        {
            // 如果是图片消息，上传图片,ChatMessage.Content为图片路径
            if (chatMessage.ContentCase == ChatMessage.ContentOneofCase.ImageMess)
            {
                var (state, message) = await UploadChatFile(userId, chatMessage.ImageMess.FilePath);

                if (!state) return (false, message);
                chatMessage.ImageMess.FilePath = message;
            }
        }

        var groupChatMessage = new GroupChatMessage
        {
            UserFromId = userId,
            GroupId = groupId
        };
        groupChatMessage.Messages.AddRange(groupChatMessages);

        var response = await _messageHelper.SendMessageWithResponse<GroupChatMessageResponse>(groupChatMessage);
        if (response is { Response: { State: true } })
            return (true, "Group message sent successfully");
        return (false, response?.Response?.Message ?? "Failed to send group message");
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

    #region Image

    /// <summary>
    /// 获取聊天图片，此操作在用户打开详细图片前调用
    /// </summary>
    /// <param name="userId">ChatMessage的来源客户的Id</param>
    /// <param name="fileName">文件名</param>
    /// <returns></returns>
    public async Task<byte[]?> GetChatImage(string userId, string fileName)
    {
        var _fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();
        var path = Path.Combine(userId, "ChatFile");
        return await _fileOperateHelper.GetFileForUser(path, fileName);
    }

    /// <summary>
    /// 获取压缩后的聊天图片，在聊天界面显示
    /// </summary>
    /// <param name="userId">ChatMessage的来源客户的Id</param>
    /// <param name="fileName">文件名</param>
    /// <returns></returns>
    public async Task<byte[]?> GetCompressedChatImage(string userId, string fileName)
    {
        var _fileIOHelper = _scopedProvider.Resolve<IFileIOHelper>();
        var path = Path.Combine("Users", userId, "ChatFile");
        return await _fileIOHelper.GetFileAsync(path, fileName);
    }

    #endregion

    /// <summary>
    /// 上传聊天图片，不会直接调用，而是在发送消息时调用
    /// 如果上传成功，返回true和文件名，否则返回false和错误信息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<(bool, string)> UploadChatImage(string userId, Bitmap bitmap, string? filename = null)
    {
        var _fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();

        var path = Path.Combine(userId, "ChatFile");

        var fileName =
            $"{DateTime.Now:yyyyMMddHHmmss}_{(string.IsNullOrWhiteSpace(filename) ? "图片" : Path.GetFileName(filename))}.png";
        var result = await _fileOperateHelper.UploadFileForUser(path, fileName, bitmap.BitmapToByteArray());

        if (result)
            return (true, fileName);
        return (false, "Failed to upload image");
    }

    /// <summary>
    /// 上传聊天文件，不会直接调用，而是在发送消息时调用
    /// 如果上传成功，返回true和文件名，否则返回false和错误信息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<(bool, string)> UploadChatFile(string userId, string filePath,
        FileProcessDto? fileProcessDto = null)
    {
        if (!System.IO.File.Exists(filePath)) return (false, "File not found");

        var _fileIOHelper = _scopedProvider.Resolve<IFileIOHelper>();

        var path = Path.Combine("Users", userId, "ChatFile");

        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(filePath)}";
        var result = await _fileIOHelper.UploadLargeFileAsync(path, fileName, filePath, fileProcessDto);

        if (result)
            return (true, fileName);
        return (false, "Failed to upload image");
    }

    #region ChatMessageOperate

    /// <summary>
    /// 处理好友消息
    /// </summary>
    /// <param name="chatMessage"></param>
    /// <returns></returns>
    public async Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage)
    {
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var chatPrivate = _mapper.Map<ChatPrivate>(chatMessage);
        var result =
            await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatPrivate.ChatId));
        if (result != null)
        {
            chatPrivate.IsReaded = result.IsReaded;
            chatPrivate.Id = result.Id;
        }

        chatPrivateRepository.Update(chatPrivate);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 批量处理好友请求
    /// </summary>
    /// <param name="chatMessages"></param>
    /// <returns></returns>
    public async Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages)
    {
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        foreach (var chatMessage in chatMessages)
        {
            var chatPrivate = _mapper.Map<ChatPrivate>(chatMessage);
            var result =
                await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatPrivate.ChatId));
            if (result != null)
            {
                chatPrivate.IsReaded = result.IsReaded;
                chatPrivate.Id = result.Id;
            }

            chatPrivateRepository.Update(chatPrivate);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    #endregion

    /// <summary>
    /// 用于更新文件下载状态,数据库中更改
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
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
    public async Task ReadAllChatMessage(string userId, string targetId)
    {
        var respository = _unitOfWork.GetRepository<ChatPrivate>();
        var unReadMessages = await respository.GetAllAsync(predicate: d =>
            d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId) && !d.IsReaded, disableTracking: false);
        foreach (var message in unReadMessages)
            message.IsReaded = true;
        await _unitOfWork.SaveChangesAsync();
        unReadMessages.Clear();
    }
}