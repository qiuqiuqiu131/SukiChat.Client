using Avalonia.Collections;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface.PackService;

/// <summary>
/// 本地批量获取群聊聊天服务接口
/// </summary>
public interface IGroupChatPackService
{
    /// <summary>
    /// 获取单个群聊聊天消息实体
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<GroupChatDto?> GetGroupChatDto(string userId, string groupId);

    /// <summary>
    /// 批量获取群聊聊天消息实体
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupChatDto>> GetGroupChatDtos(string userId);

    /// <summary>
    /// 获取部分群聊聊天消息数据
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="chatId">最近一条聊天ID</param>
    /// <param name="nextCount">聊天消息数量</param>
    /// <returns></returns>
    Task<List<GroupChatData>> GetGroupChatDataAsync(string? userId, string groupId, int chatId, int nextCount);


    /// <summary>
    /// 处理单条群聊消息，存入数据库中
    /// </summary>
    /// <param name="groupChatMessage">群聊消息proto</param>
    /// <returns></returns>
    Task<bool> GroupChatMessageOperate(GroupChatMessage groupChatMessage);

    /// <summary>
    /// 批量处理群聊消息，存入数据库中
    /// </summary>
    /// <param name="userId">当前童虎ID</param>
    /// <param name="groupChatMessages">群聊消息proto集合</param>
    /// <returns></returns>
    Task<bool> GroupChatMessagesOperate(string userId, IEnumerable<GroupChatMessage> groupChatMessages);

    /// <summary>
    /// 批量处理群聊详细信息，存入数据库中
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="chatGroupMessages">群聊详细信息proto集合</param>
    /// <returns></returns>
    Task<bool> ChatGroupDetailMessagesOperate(string userId, IEnumerable<ChatGroupDetailMessage> chatGroupMessages);
}