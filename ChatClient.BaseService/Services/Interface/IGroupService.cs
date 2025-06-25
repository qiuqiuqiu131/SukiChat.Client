using Avalonia.Media.Imaging;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 群聊关系服务接口
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// 判断用户是否是群组成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群组ID</param>
    /// <returns></returns>
    Task<bool> IsMember(string userId, string groupId);

    /// <summary>
    /// 创建群聊
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="members">选择的好友列表，将会拉取这些好友组建群聊</param>
    /// <returns></returns>
    Task<(bool, string)> CreateGroup(string userId, List<string> members);

    /// <summary>
    /// 更新群聊关系信息，如置顶、备注、消息免打扰等
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupRelationDto">群聊关系实体</param>
    /// <returns></returns>
    Task<bool> UpdateGroupRelation(string userId, GroupRelationDto groupRelationDto);

    /// <summary>
    /// 更新群聊信息，如群名、群描述等，只有群主有权修改群聊信息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupDto">群聊实体</param>
    /// <returns></returns>
    Task<bool> UpdateGroup(string userId, GroupDto groupDto);

    /// <summary>
    /// 发送加入群聊请求
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="message">请求消息</param>
    /// <param name="nickName">群聊昵称</param>
    /// <param name="grouping">群聊分组</param>
    /// <param name="remark">群聊备注</param>
    /// <returns></returns>
    Task<(bool, string)> JoinGroupRequest(string userId, string groupId, string message, string nickName,
        string grouping, string remark);

    /// <summary>
    /// 发送加入群聊的回应,用户的加群申请会被群主和群管理员接收到。
    /// 群主和群管理员有权审核用户的加群申请。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="requestId">加群请求ID</param>
    /// <param name="accept">是否同意</param>
    /// <returns></returns>
    Task<(bool, string)> JoinGroupResponse(string userId, int requestId, bool accept);

    /// <summary>
    /// 退出群聊请求
    /// </summary>
    /// <param name="userId">退群者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<(bool, string)> QuitGroupRequest(string userId, string groupId);

    /// <summary>
    /// 移除群成员，群主和群管理员有权移除群成员。
    /// 通知只能移除普通成员，不能移除群主和管理员。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="memberId">群成员ID</param>
    /// <returns></returns>
    Task<(bool, string)> RemoveMemberRequest(string userId, string groupId, string memberId);

    /// <summary>
    /// 解散群聊请求，只有群主有权解散群聊。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<(bool, string)> DisbandGroupRequest(string userId, string groupId);

    /// <summary>
    /// 编辑群聊头像，只有群主有权编辑群聊头像。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="bitmap">群头像</param>
    /// <returns></returns>
    Task<(bool, string)> EditGroupHead(string userId, string groupId, Bitmap bitmap);

    /// <summary>
    /// 获取到加入群聊请求的响应的响应
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> GetJoinGroupResponseResponseFromServer(string userId, JoinGroupResponseResponseFromServer message);

    /// <summary>
    /// 获取到加入群聊请求的响应
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<GroupRequest?> GetJoinGroupResponseFromServer(string userId, JoinGroupResponseFromServer message);

    /// <summary>
    /// 获取到加入群聊请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> GetJoinGroupRequestFromServer(string userId, GroupReceivedDto message);
}