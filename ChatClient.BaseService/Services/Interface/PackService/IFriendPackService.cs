using Avalonia.Collections;
using ChatClient.Tool.Data.Friend;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface.PackService;

/// <summary>
/// 本地批量获取好友服务接口
/// </summary>
public interface IFriendPackService
{
    /// <summary>
    /// 获取本地数据库中的朋友关系
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendRelationDto>?> GetFriendRelationDtos(string userId);

    /// <summary>
    /// 获取本地数据库中的接收到的朋友请求（Response）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendReceiveDto>?> GetFriendReceiveDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 获取本地数据库中的发送的朋友请求（Request）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 获取本地数据库中的朋友删除记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendDeleteDto>?> GetFriendDeleteDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 处理单条新朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">新好友proto</param>
    /// <returns></returns>
    Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message);

    /// <summary>
    /// 批量处理新朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="messages">新好友proto集合</param>
    /// <returns></returns>
    Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages);

    /// <summary>
    /// 处理单条删除朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">好友删除proto</param>
    /// <returns></returns>
    Task<bool> FriendDeleteMessageOperate(string userId, DeleteFriendMessage message);

    /// <summary>
    /// 批量处理删除朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="messages">好友删除proto集合</param>
    /// <returns></returns>
    Task<bool> FriendDeleteMessagesOperate(string userId, IEnumerable<FriendDeleteMessage> messages);

    /// <summary>
    /// 批量处理好友请求消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    Task<bool> FriendRequestMessagesOperate(string userId, IEnumerable<FriendRequestMessage> messages);

    /// <summary>
    /// 批量处理好友请求接受消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    Task<bool> FriendReceivedMesssagesOperate(string userId, IEnumerable<FriendRequestMessage> messages);
}