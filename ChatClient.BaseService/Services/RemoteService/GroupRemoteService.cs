using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.RemoteService;

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

internal class GroupRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IGroupGetService groupService,
    IUserService userService)
    : BaseService(containerProvider), IGroupRemoteService
{
    public async Task<List<GroupDto>> GetRemoteGroups(string userId)
    {
        var result = new List<GroupMessage>();

        // 构建请求对象
        var request = new GroupMessageListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取群聊信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GroupMessageListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Groups);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        List<Task<GroupDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(groupService.GetGroupDto(userMessage));

        await Task.WhenAll(tasks);

        return tasks.Select(d => d.Result).ToList();
    }

    public async Task<List<GroupMemberDto>> GetRemoteGroupMembers(string userId, string groupId)
    {
        var result = new List<GroupMemberMessage>();

        // 构建请求对象
        var request = new GroupMemberListRequest
        {
            UserId = userId,
            GroupId = groupId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取群成员信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GroupMemberListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Members);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        List<Task<GroupMemberDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(groupService.GetGroupMemberDto(userMessage, userService));

        await Task.WhenAll(tasks);

        return tasks.Select(d => d.Result).ToList();
    }
}