using ChatClient.BaseService.Manager;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.RemoteService;

/// <summary>
/// 批量获取远程用户信息服务接口
/// </summary>
public interface IUserRemoteService
{
    /// <summary>
    /// 批量获取远程用户信息
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<UserDto>> GetRemoteUsersAsync(string userId);
}

internal class UserRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IUserService userService)
    : BaseService(containerProvider), IUserRemoteService
{
    public async Task<List<UserDto>> GetRemoteUsersAsync(string userId)
    {
        List<UserMessage> result = new();

        // 构建请求对象
        var request = new GetUserListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取用户信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetUserListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Users);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        List<Task<UserDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(userService.GetUserDto(userMessage));

        await Task.WhenAll(tasks);

        return tasks.Select(d => d.Result).ToList();
    }
}