using System.Text.RegularExpressions;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.SearchService;

/// <summary>
/// 全网搜索服务接口
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 全网搜索用户
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="content">搜索词</param>
    /// <returns></returns>
    Task<List<UserDto>> SearchUserAsync(string userId, string content);

    /// <summary>
    /// 全网搜索群聊
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="content">搜索词</param>
    /// <returns></returns>
    Task<List<GroupDto>> SearchGroupAsync(string userId, string content);

    /// <summary>
    /// 全网搜索用户和群聊
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="content">搜索词</param>
    /// <returns></returns>
    Task<(List<UserDto>, List<GroupDto>)> SearchAllAsync(string userId, string content);
}

public class SearchService : BaseService, ISearchService
{
    private readonly IMessageHelper _messageHelper;

    public SearchService(IContainerProvider containerProvider, IMessageHelper messageHelper) : base(containerProvider)
    {
        _messageHelper = messageHelper;
    }

    public async Task<List<UserDto>> SearchUserAsync(string userId, string content)
    {
        var request = new SearchUserRequest
        {
            Content = content,
            UserId = userId
        };

        var result = await _messageHelper.SendMessageWithResponse<SearchUserResponse>(request);
        if (result is { Response: { State: true } })
        {
            var list = new List<UserDto>();
            var userService = _scopedProvider.Resolve<IUserService>();
            var friendService = _scopedProvider.Resolve<IFriendService>();
            var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
            foreach (var id in result.Ids)
            {
                var userDto = await userService.GetUserDto(id);
                if (userDto != null)
                {
                    if (userDto.Id.Equals(userId))
                        userDto.IsUser = true;
                    else if (await friendService.IsFriend(userId, userDto.Id))
                    {
                        var relation = await userDtoManager.GetFriendRelationDto(userId, userDto.Id);
                        userDto.IsFriend = true;
                        userDto.Remark = relation?.Remark;
                    }

                    list.Add(userDto);
                }
            }

            return list;
        }

        return [];
    }

    public async Task<List<GroupDto>> SearchGroupAsync(string userId, string content)
    {
        var request = new SearchGroupRequest
        {
            Content = content,
            UserId = userId
        };

        var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();

        var result = await _messageHelper.SendMessageWithResponse<SearchGroupResponse>(request);
        if (result is { Response: { State: true } })
        {
            var list = new List<GroupDto>();
            var groupGetService = _scopedProvider.Resolve<IGroupGetService>();
            var groupService = _scopedProvider.Resolve<IGroupService>();
            foreach (var id in result.Ids)
            {
                var groupDto = await groupGetService.GetGroupDto(userId, id);
                if (groupDto != null)
                {
                    if (await groupService.IsMember(userId, groupDto.Id))
                    {
                        var relation = await userDtoManager.GetGroupRelationDto(userId, groupDto.Id);
                        groupDto.IsEntered = true;
                        groupDto.Remark = relation?.Remark;
                    }
                    else
                        groupDto.IsEntered = false;

                    list.Add(groupDto);
                }
            }

            return list;
        }

        return [];
    }

    public async Task<(List<UserDto>, List<GroupDto>)> SearchAllAsync(string userId, string content)
    {
        var item1 = SearchUserAsync(userId, content);
        var item2 = SearchGroupAsync(userId, content);
        await Task.WhenAll(item1, item2);
        return (item1.Result, item2.Result);
    }
}