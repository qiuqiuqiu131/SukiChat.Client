using System.Collections;
using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IChatPackService
{
    Task<FriendChatDto> GetFriendChatDto(string userId, string targetId);
    Task<AvaloniaList<FriendChatDto>> GetFriendChatDtos(string userId);
    Task<List<ChatData>> GetChatDataAsync(string? userId, string targetId, int chatId, int nextCount);
    Task OperateChatMessage(string userFromId, int chatId, List<ChatMessageDto> chatMessages);
    Task<int> GetUnReadChatMessageCount(string userId, string targetId);
}

public class ChatPackService : BaseService, IChatPackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IFileOperateHelper _fileOperateHelper;

    public ChatPackService(IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IFileOperateHelper fileOperateHelper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _fileOperateHelper = fileOperateHelper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    /// <summary>
    /// 获取userId与好友targetId的聊天记录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    public async Task<FriendChatDto> GetFriendChatDto(string userId, string targetId)
    {
        var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var friendChat = await friendChatRepository.GetFirstOrDefaultAsync(
            predicate: d =>
                (d.UserFromId.Equals(userId) && d.UserTargetId.Equals(targetId)) ||
                (d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId)),
            orderBy: o => o.OrderByDescending(d => d.ChatId));

        FriendChatDto friendChatDto = new FriendChatDto();
        friendChatDto.UserId = targetId;
        friendChatDto.FriendRelatoinDto = await _userDtoManager.GetFriendRelationDto(userId, targetId);
        friendChatDto.UnReadMessageCount = await GetUnReadChatMessageCount(userId, targetId);

        if (friendChat == null)
            return friendChatDto;

        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        // 生成ChatData（单条聊天消息，数组，包含文字图片等）
        var chatData = mapper.Map<ChatData>(friendChat);
        chatData.IsUser = friendChat.UserFromId.Equals(userId);
        chatData.IsWriting = false;
        await OperateChatMessage(friendChat.UserFromId, chatData.ChatId, chatData.ChatMessages);

        friendChatDto.ChatMessages.Add(chatData);
        return friendChatDto;
    }

    /// <summary>
    /// 获取某个用户的所有好友的聊天记录
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<AvaloniaList<FriendChatDto>> GetFriendChatDtos(string userId)
    {
        var result = new AvaloniaList<FriendChatDto>();

        // 获取所有好友的Id
        var friendService = _scopedProvider.Resolve<IFriendService>();
        var friendIds = await friendService.GetFriendIds(userId);

        // 获取好友的聊天记录
        foreach (var friendId in friendIds)
        {
            var chatPackService = _scopedProvider.Resolve<IChatPackService>();
            result.Add(await chatPackService.GetFriendChatDto(userId, friendId));
        }

        var ordered = result.OrderByDescending(d => d.LastChatMessages?.Time).ToList();

        result.Clear();
        foreach (var chatDto in ordered)
            result.Add(chatDto);

        return result;
    }

    /// <summary>
    /// 获取部分聊天记录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <param name="chatId"></param>
    /// <param name="nextCount"></param>
    /// <returns></returns>
    public async Task<List<ChatData>> GetChatDataAsync(string? userId, string targetId, int chatId,
        int nextCount)
    {
        if (userId == null) return new List<ChatData>();

        // 从数据库中获取聊天记录
        var task = new Task<IEnumerable<ChatPrivate>>(() =>
        {
            var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();
            var friendChat = friendChatRepository.GetAll(predicate: d =>
                    ((d.UserFromId.Equals(userId) && d.UserTargetId.Equals(targetId)) ||
                     (d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId))) && d.ChatId < chatId,
                orderBy: o => o.OrderByDescending(d => d.ChatId)).Take(nextCount).ToList();

            return friendChat;
        });
        task.Start();
        await task;

        // 将ChatPrivate转换为ChatData
        var friendChat = task.Result;
        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        var chatDatas = new List<ChatData>();
        foreach (var chatPrivate in friendChat)
        {
            var data = mapper.Map<ChatData>(chatPrivate);
            data.IsUser = chatPrivate.UserFromId.Equals(userId);
            data.IsWriting = false;
            await OperateChatMessage(chatPrivate.UserFromId, data.ChatId, data.ChatMessages);
            chatDatas.Add(data);
        }

        return chatDatas;
    }

    /// <summary>
    /// 将ChatMessages中的资源注入
    /// </summary>
    /// <param name="userFromId"></param>
    /// <param name="chatMessages"></param>
    public async Task OperateChatMessage(string userFromId,
        int chatId,
        List<ChatMessageDto> chatMessages)
    {
        foreach (var chatMessage in chatMessages)
        {
            if (chatMessage.Type == ChatMessage.ContentOneofCase.ImageMess)
            {
                var messContent = (ImageMessDto)chatMessage.Content;
                string filename = messContent.FilePath;
                var content = await _fileOperateHelper.GetFileForUser(
                    Path.Combine(userFromId, "ChatFile"), filename);
                //TODO: 图片失效处理
                using (MemoryStream stream = new MemoryStream(content))
                    messContent.ImageSource = new Bitmap(stream);
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

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<int> GetUnReadChatMessageCount(string userId, string targetId)
    {
        var repository = _unitOfWork.GetRepository<ChatPrivate>();
        return repository.CountAsync(d =>
            d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId) && !d.IsReaded);
    }
}