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
    Task<List<GroupMessage>> GetRemoteGroups(string userId);

    /// <summary>
    /// 批量获取远程群成员信息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<List<GroupMemberMessage>> GetRemoteGroupMembers(string userId);
}

internal class GroupRemoteService(IContainerProvider containerProvider)
    : BaseService(containerProvider), IGroupRemoteService
{
    public Task<List<GroupMessage>> GetRemoteGroups(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<List<GroupMemberMessage>> GetRemoteGroupMembers(string userId)
    {
        throw new NotImplementedException();
    }
}