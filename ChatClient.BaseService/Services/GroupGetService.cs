using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IGroupGetService
{
    Task<List<string>> GetGroupIds(string userId);
    Task<List<string>> GetGroupChatIds(string userId);
    Task<GroupDto?> GetGroupDto(string userId, string groupId, bool loadHead = false);
    Task<List<string>?> GetGroupMemberIds(string userId, string groupId);
    Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId);
    Task<List<string>> GetGroupsOfUserManager(string userId);
    Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId);
}

public class GroupGetService : BaseService, IGroupGetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageHelper _messageHelper;
    private readonly IMapper _mapper;

    public GroupGetService(IContainerProvider containerProvider, IMapper mapper) : base(containerProvider)
    {
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
        _messageHelper = _scopedProvider.Resolve<IMessageHelper>();
    }

    public Task<List<string>> GetGroupIds(string userId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        return groupRelationRepository.GetAll(predicate: d => d.UserId.Equals(userId))
            .Select(d => d.GroupId).ToListAsync();
    }

    public Task<List<string>> GetGroupChatIds(string userId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        return groupRelationRepository.GetAll(predicate: d => d.UserId.Equals(userId) && d.IsChatting)
            .Select(d => d.GroupId).ToListAsync();
    }

    public async Task<GroupDto?> GetGroupDto(string userId, string groupId, bool loadHead = false)
    {
        var groupRequest = new GroupMessageRequest { GroupId = groupId, UserId = userId };
        var groupMessage = await _messageHelper.SendMessageWithResponse<GroupMessage>(groupRequest);
        if (groupMessage == null) return null;

        var groupDto = _mapper.Map<GroupDto>(groupMessage);

        var group = _mapper.Map<Group>(groupDto);
        var groupRepository = _unitOfWork.GetRepository<Group>();
        if (await groupRepository.ExistsAsync(d => d.Id.Equals(group.Id)))
            groupRepository.Update(group);
        else
            await groupRepository.InsertAsync(group);
        await _unitOfWork.SaveChangesAsync();

        if (!loadHead)
            _ = Task.Run(async () =>
                groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex, groupId, groupDto.IsCustomHead));
        else
            groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex, groupId, groupDto.IsCustomHead);

        return groupDto;
    }

    public async Task<List<string>?> GetGroupMemberIds(string userId, string groupId)
    {
        var memberIdsRequest = new GroupMemberIdsRequest { GroupId = groupId, UserId = userId };
        var memberIds = await _messageHelper.SendMessageWithResponse<GroupMemberIds>(memberIdsRequest);
        return memberIds?.MemberIds.ToList();
    }

    public async Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId)
    {
        var groupMemberRequest = new GroupMemberRequest { GroupId = groupId, MemberId = memberId };
        var groupMemberMessage = await _messageHelper.SendMessageWithResponse<GroupMemberMessage>(groupMemberRequest)!;
        if (groupMemberMessage == null) return null;

        var groupMemberDto = _mapper.Map<GroupMemberDto>(groupMemberMessage);
        _ = Task.Run(async () =>
        {
            var userService = _scopedProvider.Resolve<IUserService>();
            groupMemberDto.HeadImage =
                await userService.GetHeadImage(groupMemberDto.UserId, groupMemberDto.HeadIndex);
        }).ConfigureAwait(false);

        if (groupMemberDto.Status != 3)
        {
            var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
            var groupMember = _mapper.Map<GroupMember>(groupMemberDto);
            var entity = await groupMemberRepository.GetFirstOrDefaultAsync(
                predicate: d => d.GroupId.Equals(groupId) && d.UserId.Equals(memberId));
            if (entity != null)
                groupMember.Id = entity.Id;
            groupMemberRepository.Update(groupMember);
            await _unitOfWork.SaveChangesAsync();
        }

        return groupMemberDto;
    }

    public Task<List<string>> GetGroupsOfUserManager(string userId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        return groupRelationRepository
            .GetAll(predicate: d => d.UserId.Equals(userId) && (d.Status == 0 || d.Status == 1))
            .Select(d => d.GroupId).ToListAsync();
    }

    /// <summary>
    /// 从数据库中获取User和Group的Relation
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        var groupRelation = await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(groupId) && d.UserId.Equals(userId));
        if (groupRelation == null) return null;
        var dto = _mapper.Map<GroupRelationDto>(groupRelation);
        return dto;
    }

    // 获取头像
    private async Task<Bitmap> GetHeadImage(int headIndex, string groupId, bool isCustom = false)
    {
        var imageManager = _scopedProvider.Resolve<IImageManager>();
        if (!isCustom)
            return await imageManager.GetGroupFile("HeadImage", $"{headIndex}.png")!;
        else
        {
            imageManager.RemoveFromCache(groupId, "HeadImage", $"{groupId}.png", FileTarget.Group);
            var image = await imageManager.GetFile(groupId, "HeadImage", $"{groupId}.png", FileTarget.Group)!;
            if (image == null)
                return await imageManager.GetGroupFile("HeadImage", $"{headIndex}.png")!;
            return image;
        }
    }

    public Task<Dictionary<int, Bitmap>> GetHeadImages()
    {
        throw new NotImplementedException();
    }
}