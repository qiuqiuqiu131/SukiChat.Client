using Avalonia.Collections;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface.PackService;

/// <summary>
/// 本地批量获取好友聊天服务接口
/// </summary>
public interface IFriendChatPackService
{
    /// <summary>
    /// 获取单个好友聊天记录
    /// </summary>
    /// <param name="userId">当前用户Id</param>
    /// <param name="targetId">目标用户ID</param>
    /// <returns></returns>
    Task<FriendChatDto?> GetFriendChatDto(string userId, string targetId);

    /// <summary>
    /// 批量获取好友聊天记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendChatDto>> GetFriendChatDtos(string userId);

    /// <summary>
    /// 获取部分聊天记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <param name="chatId">开始聊天记录，聊天记录部分会从此聊天记录开始往后截取</param>
    /// <param name="nextCount">聊天记录数量</param>
    /// <returns></returns>
    Task<List<ChatData>> GetFriendChatDataAsync(string? userId, string targetId, int chatId, int nextCount);

    /// <summary>
    /// 处理Protobuf的好友聊天消息，并保存到数据库
    /// </summary>
    /// <param name="chatMessage">聊天消息</param>
    /// <returns></returns>
    Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage);

    /// <summary>
    /// 批量处理Protobuf的好友聊天消息，并保存到数据库
    /// </summary>
    /// <param name="chatMessages"></param>
    /// <returns></returns>
    Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages);

    /// <summary>
    /// 处理聊天具体信息，并保存到数据库（聊天具体信息：是否被撤回、是否删除等）
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="chatPrivateDetailMessages"></param>
    /// <returns></returns>
    Task<bool> ChatPrivateDetailMessagesOperate(IEnumerable<ChatPrivateDetailMessage> chatPrivateDetailMessages);
}