using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.SearchData;

namespace ChatClient.BaseService.Services.Interface.SearchService;

/// <summary>
/// 本地搜索服务接口
/// </summary>
public interface ILocalSearchService
{
    /// <summary>
    /// 本地搜索好友和群聊
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchWord">搜索词</param>
    /// <returns></returns>
    public Task<AllSearchDto> SearchAllAsync(string userId, string searchWord);

    /// <summary>
    /// 本地搜索好友
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchWord">搜索词</param>
    /// <param name="limit">数量限制</param>
    /// <returns></returns>
    public Task<List<FriendSearchDto>> SearchFriendAsync(string userId, string searchWord, int limit = int.MaxValue);

    /// <summary>
    /// 本地搜索群组
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchWord">搜索词</param>
    /// <param name="limit">数量限制</param>
    /// <returns></returns>
    public Task<List<GroupSearchDto>> SearchGroupAsync(string userId, string searchWord, int limit = int.MaxValue);

    /// <summary>
    /// 获取本地用户聊天文件搜索结果
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchWord">搜索词</param>
    /// <param name="limit">限制</param>
    /// <returns></returns>
    public Task<List<FileMessDto>> SearchFileAsync(string userId, string searchWord, int limit = int.MaxValue);
}