using ChatClient.BaseService.Helper;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface ISearchService
{
    Task<List<UserDto>> SearchUserAsync(string userId, string content);
    Task<List<GroupDto>> SearchGroupAsync(string userId, string content);
    Task<List<object>> SearchAllAsync(string userId, string content);
}

public class SearchService : ISearchService
{
    private readonly IContainerProvider _containerProvider;
    private readonly IMessageHelper _messageHelper;

    public SearchService(IContainerProvider containerProvider, IMessageHelper messageHelper)
    {
        _containerProvider = containerProvider;
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
            var userService = _containerProvider.Resolve<IUserService>();
            var friendService = _containerProvider.Resolve<IFriendService>();
            foreach (var id in result.Ids)
            {
                var userDto = await userService.GetUserDto(id);
                if (userDto != null)
                {
                    if (userDto.Id.Equals(userId))
                        userDto.IsUser = true;
                    else if (await friendService.IsFriend(userId, userDto.Id))
                        userDto.IsFriend = true;

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

        var result = await _messageHelper.SendMessageWithResponse<SearchGroupResponse>(request);
        if (result is { Response: { State: true } })
        {
            var list = new List<GroupDto>();
            var groupGetService = _containerProvider.Resolve<IGroupGetService>();
            var groupService = _containerProvider.Resolve<IGroupService>();
            foreach (var id in result.Ids)
            {
                var userDto = await groupGetService.GetGroupDto(userId, id);
                if (userDto != null)
                {
                    if (await groupService.IsMember(userId, userDto.Id))
                        userDto.IsEntered = true;
                    else
                        userDto.IsEntered = false;
                    list.Add(userDto);
                }
            }

            return list;
        }

        return [];
    }

    public async Task<List<object>> SearchAllAsync(string userId, string content)
    {
        List<object> result = new();
        result.AddRange(await SearchGroupAsync(userId, content));
        result.AddRange(await SearchUserAsync(userId, content));
        return result;
    }
}