using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.Services.ServiceSugar;

public class GroupGetSugarService : IGroupGetService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly IMessageHelper _messageHelper;
    private readonly IContainerProvider _containerProvider;
    private readonly IMapper _mapper;

    public GroupGetSugarService(IContainerProvider containerProvider,
        ISqlSugarClient sqlSugarClient,
        IMessageHelper messageHelper,
        IMapper mapper)
    {
        _containerProvider = containerProvider;
        _mapper = mapper;
        _messageHelper = messageHelper;
        _sqlSugarClient = sqlSugarClient;
    }

    public Task<List<string>> GetGroupIds(string userId)
    {
        return _sqlSugarClient.Queryable<GroupRelation>()
            .Where(d => d.UserId == userId)
            .Select(d => d.GroupId).ToListAsync();
    }

    public Task<List<string>> GetGroupChatIds(string userId)
    {
        return _sqlSugarClient.Queryable<GroupRelation>()
            .Where(d => d.UserId == userId && d.IsChatting)
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

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var group = _mapper.Map<Group>(groupDto);
            var groupRepository = _unitOfWork.GetRepository<Group>();
            await groupRepository.InsertOrUpdateAsync(group);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
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
            var userService = _containerProvider.Resolve<IUserService>();
            groupMemberDto.HeadImage =
                await userService.GetHeadImage(groupMemberDto.UserId, groupMemberDto.HeadIndex);
        }).ConfigureAwait(false);

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupMember = _mapper.Map<GroupMember>(groupMemberDto);
            var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
            await groupMemberRepository.InsertOrUpdateAsync(groupMember);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            throw;
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
        return _sqlSugarClient.Queryable<GroupRelation>()
            .Where(d => d.UserId == userId && (d.Status == 0 || d.Status == 1))
            .Select(d => d.GroupId).ToListAsync();
    }

    public async Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId)
    {
        var groupRelation = await _sqlSugarClient.Queryable<GroupRelation>()
            .FirstAsync(d => d.GroupId == groupId && d.UserId == userId);
        if (groupRelation == null) return null;
        var dto = _mapper.Map<GroupRelationDto>(groupRelation);
        return dto;
    }

    private async Task<Bitmap> GetHeadImage(int headIndex, string groupId, bool isCustom = false)
    {
        var imageManager = _containerProvider.Resolve<IImageManager>();
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
        var entity = await _sqlSugarClient.Queryable<GroupRelation>()
            .FirstAsync(d => d.UserId == userId && d.Grouping == groupId);
        return entity?.Grouping;
    }
}