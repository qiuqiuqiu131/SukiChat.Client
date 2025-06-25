using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.SearchData;
using SqlSugar;

namespace ChatClient.BaseService.Services.ServiceSugar.SearchServie;

public class LocalSearchSugarService(ISqlSugarClient sqlSugarClient, IUserDtoManager userDtoManager)
    : ILocalSearchService
{
    public async Task<AllSearchDto> SearchAllAsync(string userId, string searchWord)
    {
        if (string.IsNullOrWhiteSpace(searchWord))
        {
            return new AllSearchDto
            {
                FriendSearchDtos = new List<FriendSearchDto>(),
                GroupSearchDtos = new List<GroupSearchDto>()
            };
        }

        // 获取最多3条好友搜索结果
        var friendResults = await SearchFriendAsync(userId, searchWord, 3);

        // 获取最多3条群组搜索结果
        var groupResults = await SearchGroupAsync(userId, searchWord, 3);

        return new AllSearchDto
        {
            FriendSearchDtos = friendResults,
            GroupSearchDtos = groupResults
        };
    }

    public async Task<List<FriendSearchDto>> SearchFriendAsync(string userId, string searchWord,
        int limit = int.MaxValue)
    {
        var ids = await sqlSugarClient.Queryable<FriendRelation>()
            .Where(f => f.User1Id == userId)
            .LeftJoin<User>((f, u) => f.User2Id == u.Id)
            .Where((f, u) =>
                !SqlFunc.IsNullOrEmpty(f.Remark) &&
                f.Remark.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || !SqlFunc.IsNullOrEmpty(u.Name) &&
                u.Name.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || searchWord.Length > 3 && f.User2Id.Contains(searchWord))
            .OrderBy(f => f.User2Id)
            .Select(f => f.User2Id)
            .Distinct()
            .Take(limit).ToListAsync();

        var result = new List<FriendSearchDto>();
        foreach (var id in ids)
        {
            var dto = await userDtoManager.GetFriendRelationDto(userId, id);
            if (dto != null)
                result.Add(new FriendSearchDto
                {
                    FriendRelationDto = dto,
                    Message = $"来自：{dto.grouping}"
                });
        }

        return result;
    }

    public async Task<List<GroupSearchDto>> SearchGroupAsync(string userId, string searchWord, int limit = int.MaxValue)
    {
        var l = await sqlSugarClient.Queryable<GroupRelation>()
            .Where(f => f.UserId == userId) // 添加用户ID过滤
            .LeftJoin<Group>((f, g) => f.GroupId == g.Id)
            .Where((f, g) =>
                !SqlFunc.IsNullOrEmpty(f.Remark) &&
                f.Remark.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || !SqlFunc.IsNullOrEmpty(g.Name) &&
                g.Name.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || searchWord.Length > 3 && f.UserId.Contains(searchWord)).ToListAsync();

        var ids = await sqlSugarClient.Queryable<GroupRelation>()
            .Where(f => f.UserId == userId) // 添加用户ID过滤
            .LeftJoin<Group>((f, g) => f.GroupId == g.Id)
            .Where((f, g) =>
                !SqlFunc.IsNullOrEmpty(f.Remark) &&
                f.Remark.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || !SqlFunc.IsNullOrEmpty(g.Name) &&
                g.Name.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase)
                || searchWord.Length > 3 && f.UserId.Contains(searchWord))
            .OrderBy(f => f.GroupId)
            .Select(f => f.GroupId)
            .Distinct()
            .Take(limit).ToListAsync();

        var result = new List<GroupSearchDto>();
        foreach (var id in ids)
        {
            var dto = await userDtoManager.GetGroupRelationDto(userId, id);
            if (dto != null)
                result.Add(new GroupSearchDto
                {
                    GroupRelationDto = dto,
                    Message = $"来自：{dto.Grouping}"
                });
        }

        return result;
    }

    public async Task<List<FileMessDto>> SearchFileAsync(string userId, string searchWord, int limit = Int32.MaxValue)
    {
        return null;
    }
}