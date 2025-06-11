using ChatClient.BaseService.Manager;
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
    Task<List<UserMessage>> GetRemoteUsersAsync(string userId);
}

internal class UserRemoteService(IContainerProvider containerProvider)
    : BaseService(containerProvider), IUserRemoteService
{
    public Task<List<UserMessage>> GetRemoteUsersAsync(string userId)
    {
        throw new NotImplementedException();
    }
}