using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

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

public class UserGroupService : IUserGroupService
{
    private readonly IMessageHelper _messageHelper;
    private readonly IUnitOfWork _unitOfWork;

    public UserGroupService(IMessageHelper messageHelper, IUnitOfWork unitOfWork)
    {
        _messageHelper = messageHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> AddGroupAsync(string userId, string groupName, int groupType)
    {
        var request = new AddUserGroupRequest
        {
            UserGroup = new UserGroupMessage
            {
                UserId = userId,
                GroupName = groupName,
                GroupType = groupType
            }
        };

        var response = await _messageHelper.SendMessageWithResponse<AddUserGroupResponse>(request);
        if (response is { Response: { State: true } })
            return true;

        return false;
    }

    public async Task<bool> RenameGroupAsync(string userId, string oldGroupName, string newGroupName, int groupType)
    {
        var request = new RenameUserGroupRequest
        {
            UserId = userId,
            UserGroup = new UserGroupMessage
            {
                UserId = userId,
                GroupName = oldGroupName,
                GroupType = groupType
            },
            NewGroupName = newGroupName
        };

        var response = await _messageHelper.SendMessageWithResponse<RenameUserGroupResponse>(request);
        if (response is { Response: { State: true } })
        {
            if (groupType == 0)
            {
                var repository = _unitOfWork.GetRepository<FriendRelation>();
                var relations = await repository.GetAllAsync(
                    predicate: d => d.User1Id.Equals(userId) && d.Grouping.Equals(oldGroupName),
                    disableTracking: false);
                foreach (var relation in relations)
                    relation.Grouping = newGroupName;

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
            else if (groupType == 1)
            {
                var repository = _unitOfWork.GetRepository<GroupRelation>();
                var relations = await repository.GetAllAsync(
                    predicate: d => d.UserId.Equals(userId) && d.Grouping.Equals(oldGroupName),
                    disableTracking: false);
                foreach (var relation in relations)
                    relation.Grouping = newGroupName;

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            return true;
        }

        return false;
    }

    public async Task<bool> DeleteGroupAsync(string userId, string groupName, int groupType)
    {
        var request = new DeleteUserGroupRequest
        {
            UserGroup = new UserGroupMessage
            {
                UserId = userId,
                GroupName = groupName,
                GroupType = groupType
            }
        };

        var response = await _messageHelper.SendMessageWithResponse<DeleteUserGroupResponse>(request);
        if (response is { Response: { State: true } })
        {
            if (groupType == 0)
            {
                var repository = _unitOfWork.GetRepository<FriendRelation>();
                var relations = await repository.GetAllAsync(
                    predicate: d => d.User1Id.Equals(userId) && d.Grouping.Equals(groupName),
                    disableTracking: false);
                foreach (var relation in relations)
                    relation.Grouping = "默认分组";

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
            else if (groupType == 1)
            {
                var repository = _unitOfWork.GetRepository<GroupRelation>();
                var relations = await repository.GetAllAsync(
                    predicate: d => d.UserId.Equals(userId) && d.Grouping.Equals(groupName),
                    disableTracking: false);
                foreach (var relation in relations)
                    relation.Grouping = "默认分组";

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            return true;
        }

        return false;
    }
}