using Avalonia.Collections;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using Google.Protobuf;

namespace ChatClient.BaseService.Services.Interface.PackService;

/// <summary>
/// 本地批量获取群聊服务接口
/// </summary>
public interface IGroupPackService
{
    /// <summary>
    /// 批量获取群聊关系实体
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupRelationDto>?> GetGroupRelationDtos(string userId);

    /// <summary>
    /// 批量获取入群请求实体（发送的）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupRequestDto>?> GetGroupRequestDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 批量获取入群请求实体 （接受的）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 批量获取群聊删除消息实体，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupDeleteDto>?> GetGroupDeleteDtos(string userId, DateTime lastDeleteTime);


    /// <summary>
    /// 处理单条被拉入群聊消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">被拉入群proto</param>
    /// <returns></returns>
    Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message);

    /// <summary>
    /// 批量处理入群消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="enterGroupMessages">入群消息集合</param>
    /// <returns></returns>
    Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages);

    /// <summary>
    /// 处理单条群聊删除消息，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">prot消息</param>
    /// <returns></returns>
    Task<bool> GroupDeleteMessageOperate(string userId, IMessage message);

    /// <summary>
    /// 批量处理群聊删除消息，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupDeleteMessages">移除群聊proto集合</param>
    /// <returns></returns>
    Task<bool> GroupDeleteMessagesOperate(string userId, IEnumerable<GroupDeleteMessage> groupDeleteMessages);

    /// <summary>
    /// 批量处理好友请求消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    Task<bool> GroupRequestMessagesOperate(string userId, IEnumerable<GroupRequestMessage> messages);

    /// <summary>
    /// 批量处理好友请求接受消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    Task<bool> GroupReceivedMesssagesOperate(string userId, IEnumerable<GroupRequestMessage> messages);
}