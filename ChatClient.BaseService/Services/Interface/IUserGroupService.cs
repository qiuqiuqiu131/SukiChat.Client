namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 分组服务接口
/// </summary>
public interface IUserGroupService
{
    /// <summary>
    /// 新建分组
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupName">分组名</param>
    /// <param name="groupType">分组目标（0：好友，1：群聊）</param>
    /// <returns></returns>
    Task<bool> AddGroupAsync(string userId, string groupName, int groupType);

    /// <summary>
    /// 重命名分组
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="oldGroupName">旧分组名</param>
    /// <param name="newGroupName">新分组名</param>
    /// <param name="groupType">分组目标（0：好友，1：群聊）</param>
    /// <returns></returns>
    Task<bool> RenameGroupAsync(string userId, string oldGroupName, string newGroupName, int groupType);

    /// <summary>
    /// 删除分组
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupName">目标分组名</param>
    /// <param name="groupType">分组目标（0：好友，1：群聊）</param>
    /// <returns></returns>
    Task<bool> DeleteGroupAsync(string userId, string groupName, int groupType);
}