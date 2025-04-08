using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.SearchData;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface ILocalSearchService
{
    public Task<AllSearchDto> SearchAllAsync(string userId, string searchWord);

    public Task<List<FriendSearchDto>> SearchFriendAsync(string userId, string searchWord, int limit = int.MaxValue);

    public Task<List<GroupSearchDto>> SearchGroupAsync(string userId, string searchWord, int limit = int.MaxValue);

    public Task<List<FileMessDto>> SearchFileAsync(string userId, string searchWord, int limit = int.MaxValue);
}

public class LocalSearchService : BaseService, ILocalSearchService
{
    private readonly IUserDtoManager _userDtoManager;

    public LocalSearchService(IContainerProvider containerProvider, IUserDtoManager userDtoManager) : base(
        containerProvider)
    {
        _userDtoManager = userDtoManager;
    }

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
        var unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();

        var friendRelationRepository = unitOfWork.GetRepository<FriendRelation>();
        var friendRelations = friendRelationRepository.GetAll(predicate: d => d.User1Id.Equals(userId));

        var userRepository = unitOfWork.GetRepository<User>();
        var users = userRepository.GetAll();

        var friendQuery = (from fr in friendRelations
            join u in users on fr.User2Id equals u.Id into temp
            from u in temp.DefaultIfEmpty()
            where (fr.Remark != null && fr.Remark.ToLower().Contains(searchWord.ToLower()))
                  || (u != null && u.Name.ToLower().Contains(searchWord.ToLower()))
                  || (searchWord.Length > 3 && fr.User2Id.Contains(searchWord))
            orderby fr.User2Id
            select fr.User2Id).Distinct().Take(limit);

        var ids = await friendQuery.ToListAsync();
        var result = new List<FriendSearchDto>();
        foreach (var id in ids)
        {
            var dto = await _userDtoManager.GetFriendRelationDto(userId, id);
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
        var unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();

        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
        var groupRelations = groupRelationRepository.GetAll(predicate: d => d.UserId.Equals(userId));

        var groupRepository = unitOfWork.GetRepository<Group>();
        var groups = groupRepository.GetAll();

        var groupQuery = (from gr in groupRelations
            join g in groups on gr.GroupId equals g.Id into temp
            from g in temp.DefaultIfEmpty()
            where (gr.Remark != null && gr.Remark.ToLower().Contains(searchWord.ToLower()))
                  || (g != null && g.Name.ToLower().Contains(searchWord.ToLower()))
                  || (searchWord.Length > 3 && gr.GroupId.Contains(searchWord))
            orderby gr.GroupId
            select gr.GroupId).Distinct().Take(limit);

        var ids = await groupQuery.ToListAsync();
        var result = new List<GroupSearchDto>();
        foreach (var id in ids)
        {
            var dto = await _userDtoManager.GetGroupRelationDto(userId, id);
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