using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.Services.ServiceSugar;

public class UserGroupSugarService : IUserGroupService
{
    private readonly IMessageHelper _messageHelper;
    private readonly ISqlSugarClient _sqlSugarClient;

    public UserGroupSugarService(IMessageHelper messageHelper, ISqlSugarClient sqlSugarClient)
    {
        _messageHelper = messageHelper;
        _sqlSugarClient = sqlSugarClient;
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
                using var unitOfWork = _sqlSugarClient.CreateContext();
                try
                {
                    var relations = await unitOfWork.Db.Queryable<FriendRelation>()
                        .Where(d => d.User1Id.Equals(userId) && d.Grouping.Equals(oldGroupName)).ToListAsync();
                    // 更改单列分组值
                    await unitOfWork.Db.Updateable(relations)
                        .PublicSetColumns(d => d.Grouping, d => newGroupName)
                        .UpdateColumns(d => new { d.Grouping })
                        .ExecuteCommandAsync();
                    unitOfWork.Commit();
                }
                catch (Exception e)
                {
                    await unitOfWork.Tenant.RollbackTranAsync();
                    Console.WriteLine(e);
                    return false;
                }
            }
            else if (groupType == 1)
            {
                using var unitOfWork = _sqlSugarClient.CreateContext();
                try
                {
                    var relations = await unitOfWork.Db.Queryable<GroupRelation>()
                        .Where(d => d.UserId.Equals(userId) && d.Grouping.Equals(oldGroupName)).ToListAsync();
                    // 更改单列分组值
                    await unitOfWork.Db.Updateable(relations)
                        .PublicSetColumns(d => d.Grouping, d => newGroupName)
                        .UpdateColumns(d => new { d.Grouping })
                        .ExecuteCommandAsync();
                    unitOfWork.Commit();
                }
                catch (Exception e)
                {
                    await unitOfWork.Tenant.RollbackTranAsync();
                    Console.WriteLine(e);
                    return false;
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
                using var unitOfWork = _sqlSugarClient.CreateContext();
                try
                {
                    var relations = await unitOfWork.Db.Queryable<FriendRelation>()
                        .Where(d => d.User1Id.Equals(userId) && d.Grouping.Equals(groupName)).ToListAsync();
                    // 更改单列分组值
                    await unitOfWork.Db.Updateable(relations)
                        .PublicSetColumns(d => d.Grouping, d => "默认分组")
                        .UpdateColumns(d => new { d.Grouping })
                        .ExecuteCommandAsync();
                    unitOfWork.Commit();
                }
                catch (Exception e)
                {
                    await unitOfWork.Tenant.RollbackTranAsync();
                    Console.WriteLine(e);
                    return false;
                }
            }
            else if (groupType == 1)
            {
                using var unitOfWork = _sqlSugarClient.CreateContext();
                try
                {
                    var relations = await unitOfWork.Db.Queryable<GroupRelation>()
                        .Where(d => d.UserId.Equals(userId) && d.Grouping.Equals(groupName)).ToListAsync();
                    // 更改单列分组值
                    await unitOfWork.Db.Updateable(relations)
                        .PublicSetColumns(d => d.Grouping, d => "默认分组")
                        .UpdateColumns(d => new { d.Grouping })
                        .ExecuteCommandAsync();
                    unitOfWork.Commit();
                }
                catch (Exception e)
                {
                    await unitOfWork.Tenant.RollbackTranAsync();
                    Console.WriteLine(e);
                    return false;
                }
            }

            return true;
        }

        return false;
    }
}