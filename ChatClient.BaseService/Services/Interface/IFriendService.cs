using ChatClient.DataBase.Data;
using ChatClient.Tool.Data.Friend;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 好友处理接口
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// 判断是否是好友关系
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <returns></returns>
    public Task<bool> IsFriend(string userId, string targetId);

    /// <summary>
    /// 发送好友请求
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <param name="remark">备注</param>
    /// <param name="group">分组</param>
    /// <param name="message">请求消息</param>
    /// <returns></returns>
    public Task<(bool, string)> AddFriend(string userId, string targetId, string remark = "", string group = "默认分组",
        string message = "");

    /// <summary>
    /// 回应好友请求
    /// </summary>
    /// <param name="requestId">好友请求ID</param>
    /// <param name="state">是否接受</param>
    /// <param name="remark">如果接受，可以设置备注</param>
    /// <param name="group">如果接受，可以设置分组</param>
    /// <returns></returns>
    public Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string remark = "",
        string group = "默认分组");

    /// <summary>
    /// 获取用户的所有好友ID
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    public Task<List<string>> GetFriendIds(string userId);

    /// <summary>
    /// 获取用户的所有好友ID（仅聊天列表中的好友）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    public Task<List<string>> GetFriendChatIds(string userId);

    /// <summary>
    /// 获取用户关系实体（FriendRelationDto）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendId">好友ID</param>
    /// <returns></returns>
    public Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);

    /// <summary>
    /// 更新好友关系，如分组、备注、免打扰、置顶等
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendRelationDto">好友关系实体</param>
    /// <returns></returns>
    public Task<bool> UpdateFriendRelation(string userId, FriendRelationDto friendRelationDto);

    /// <summary>
    /// 删除好友关系
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendId">好友ID</param>
    /// <returns></returns>
    public Task<bool> DeleteFriend(string userId, string friendId);

    /// <summary>
    /// 获取好友所属的分组名称
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    /// <returns></returns>
    public Task<string?> GetFriendGroupName(string userId, string friendId);

    /// <summary>
    /// 接收到服务器的好友请求消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task<bool> GetFriendRequestFromServer(string userId, FriendRequestFromServer message);

    /// <summary>
    /// 接受到服务器的好友回应消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task<FriendRequest?> GetFriendResponseFromServer(string userId, FriendResponseFromServer message);
}