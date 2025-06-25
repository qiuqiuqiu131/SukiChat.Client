using ChatClient.Tool.Data;

namespace ChatClient.BaseService.Services.Interface.RemoteService;

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