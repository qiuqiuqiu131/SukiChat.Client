using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.BaseService.Services.Interface.SearchService;

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