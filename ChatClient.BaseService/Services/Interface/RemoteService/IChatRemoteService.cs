using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface.RemoteService;

public interface IChatRemoteService
{
    /// <summary>
    /// 批量获取好友聊天记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<FriendChatMessage>> GetFriendChatMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取群聊聊天记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<GroupChatMessage>> GetGroupChatMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取好友聊天详情信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<ChatPrivateDetailMessage>> GetChatPrivateDetailMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取群聊聊天详情信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<ChatGroupDetailMessage>> GetChatGroupDetailMessages(string userId, DateTime loginTime);
}