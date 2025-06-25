using ChatClient.Tool.Data.Group;

namespace ChatClient.BaseService.Services.Interface.RemoteService;

/// <summary>
/// 批量获取远程群聊信息服务接口
/// </summary>
public interface IGroupRemoteService
{
    /// <summary>
    /// 批量获取远程群聊信息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<List<GroupDto>> GetRemoteGroups(string userId);

    /// <summary>
    /// 批量获取远程群成员信息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupId">目标群聊ID</param>
    /// <returns></returns>
    Task<List<GroupMemberDto>> GetRemoteGroupMembers(string userId, string groupId);
}