using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.EfCore;

public class GroupGetService : Services.BaseService, IGroupGetService
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
        if (!loadHead)
            _ = Task.Run(async () =>
                groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex, groupId, groupDto.IsCustomHead));
        else
            groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex, groupId, groupDto.IsCustomHead);


        try
        {
            var group = _mapper.Map<Group>(groupDto);
            var groupRepository = _unitOfWork.GetRepository<Group>();
            groupRepository.Update(group);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
        }

        return groupDto;
    }

    public async Task<GroupDto> GetGroupDto(GroupMessage groupMessage)
    {
        var groupDto = _mapper.Map<GroupDto>(groupMessage);

        _ = Task.Run(async () =>
            groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex, groupMessage.GroupId, groupDto.IsCustomHead));

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

        try
        {
            var groupMember = _mapper.Map<GroupMember>(groupMemberDto);
            var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
            groupMemberRepository.Update(groupMember);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
        }

        return groupMemberDto;
    }

    public async Task<GroupMemberDto> GetGroupMemberDto(GroupMemberMessage memberMessage, IUserService userService)
    {
        var groupMemberDto = _mapper.Map<GroupMemberDto>(memberMessage);

        _ = Task.Run(async () => groupMemberDto.HeadImage =
            await userService.GetHeadImage(groupMemberDto.UserId, groupMemberDto.HeadIndex));

        return groupMemberDto;
    }

    public Task<List<string>> GetGroupsOfUserManager(string userId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        return groupRelationRepository
            .GetAll(predicate: d => d.UserId.Equals(userId) && (d.Status == 0 || d.Status == 1))
            .Select(d => d.GroupId).ToListAsync();
    }

    public async Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        var groupRelation = await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(groupId) && d.UserId.Equals(userId));
        if (groupRelation == null) return null;
        var dto = _mapper.Map<GroupRelationDto>(groupRelation);
        return dto;
    }

    private async Task<Bitmap> GetHeadImage(int headIndex, string groupId, bool isCustom = false)
    {
        var imageManager = _scopedProvider.Resolve<IImageManager>();
        try
        {
            if (!isCustom)
            {
                var image = await imageManager.GetGroupFile("HeadImage", $"{headIndex}.png")!;
                if (image == null)
                    image = await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                        "DefaultGroupHead.png"));
                return image;
            }
            else
            {
                imageManager.RemoveFromCache(groupId, "HeadImage", $"{groupId}.png", FileTarget.Group);
                var image = await imageManager.GetFile(groupId, "HeadImage", $"{groupId}.png", FileTarget.Group)!;
                if (image == null)
                {
                    image = await imageManager.GetGroupFile("HeadImage", $"{headIndex}.png")!;
                    if (image == null)
                        image = await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                            "DefaultGroupHead.png"));
                }

                return image;
            }
        }
        catch (Exception e)
        {
            return await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                "DefaultGroupHead.png"));
        }
    }

    public async Task<string?> GetGroupGroupName(string userId, string groupId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        var entity = await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(groupId) && d.UserId.Equals(userId));
        return entity?.Grouping;
    }
}