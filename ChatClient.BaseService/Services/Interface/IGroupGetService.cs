using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 群聊实体获取服务接口
/// </summary>
public interface IGroupGetService
{
    /// <summary>
    /// 获取用户所在的所有群组ID
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<List<string>> GetGroupIds(string userId);

    /// <summary>
    /// 获取用户所在的所有群组ID（仅聊天列表中的群组）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<List<string>> GetGroupChatIds(string userId);

    /// <summary>
    /// 获取群聊实体（GroupDto,非本地数据库访问）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="loadHead">是否需要加载群聊头像</param>
    /// <returns></returns>
    Task<GroupDto?> GetGroupDto(string userId, string groupId, bool loadHead = false);

    /// <summary>
    /// 根据GroupMessage获取群聊实体
    /// </summary>
    /// <param name="groupMessage">proto消息</param>
    /// <returns></returns>
    Task<GroupDto> GetGroupDto(GroupMessage groupMessage);

    /// <summary>
    /// 获取群组成员ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<List<string>?> GetGroupMemberIds(string userId, string groupId);

    /// <summary>
    /// 获取群组成员实体（GroupMemberDto,非本地数据库访问)
    /// </summary>
    /// <param name="groupId">群聊ID</param>
    /// <param name="memberId">成员ID</param>
    /// <returns></returns>
    Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId);

    /// <summary>
    /// 根据GroupMemberMessage获取群组成员实体（GroupMemberDto,非本地数据库访问)
    /// </summary>
    /// <param name="memberMessage"></param>
    /// <returns></returns>
    Task<GroupMemberDto> GetGroupMemberDto(GroupMemberMessage memberMessage, IUserService userService);

    /// <summary>
    /// 获取用户管理的群组ID列表（用户为群主或者群管理员）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<List<string>> GetGroupsOfUserManager(string userId);

    /// <summary>
    /// 获取群聊关系实体（GetGroupRelationDto）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId);

    /// <summary>
    /// 获取群聊所属的分组名称
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    Task<string?> GetGroupGroupName(string userId, string groupId);
}