using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using DryIoc.ImTools;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IGroupService
{
    Task<List<string>> GetGroupIds(string userId);
    Task<GroupDto?> GetGroupDto(string userId, string groupId);
    Task<List<string>?> GetGroupMemberIds(string userId, string groupId);
    Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId);
    Task<List<string>> GetGroupsOfUserManager(string userId);
    Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId);
    Task<(bool, string)> CreateGroup(string userId, List<string> members);
    Task<bool> UpdateGroupRelation(string userId, GroupRelationDto groupRelationDto);
    Task<bool> UpdateGroup(string userId, GroupDto groupDto);
    Task<(bool, string)> JoinGroupRequest(string userId, string groupId);
    Task<(bool, string)> JoinGroupResponse(string userId, int requestId, bool accept);
    Task<Bitmap> GetHeadImage(int headIndex);
    Task<Dictionary<int, Bitmap>> GetHeadImages();
}

public class GroupService : BaseService, IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageHelper _messageHelper;

    public GroupService(IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
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

    public async Task<GroupDto?> GetGroupDto(string userId, string groupId)
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

        groupDto.HeadImage = await GetHeadImage(groupDto.HeadIndex);

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
        var userService = _scopedProvider.Resolve<IUserService>();
        _ = Task.Run(async () =>
        {
            groupMemberDto.HeadImage =
                await userService.GetHeadImage(groupMemberDto.UserId, groupMemberDto.HeadIndex);
        });

        var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
        var groupMember = _mapper.Map<GroupMember>(groupMemberDto);
        var entity = await groupMemberRepository.GetFirstOrDefaultAsync(
            predicate: d => d.GroupId.Equals(groupId) && d.UserId.Equals(memberId));
        if (entity != null)
            groupMember.Id = entity.Id;
        groupMemberRepository.Update(groupMember);
        await _unitOfWork.SaveChangesAsync();

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

    /// <summary>
    /// 向服务器发送创建群组请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    /// <returns>bool:是否正确处理请求,string:正确返回组群ID，错误返回报错信息</returns>
    public async Task<(bool, string)> CreateGroup(string userId, List<string> members)
    {
        var createGroupRequest = new CreateGroupRequest
        {
            UserId = userId,
            FriendId = { members }
        };

        // 发送建群请求
        var response = await _messageHelper.SendMessageWithResponse<CreateGroupResponse>(createGroupRequest);

        if (response is { Response: { State: true } })
        {
            // 创建群组成功
            var time = DateTime.Parse(response.Time);
            try
            {
                var groupRepository = _unitOfWork.GetRepository<Group>();
                await groupRepository.InsertAsync(new Group
                {
                    Id = response.GroupId,
                    Name = response.GroupName,
                    CreateTime = time,
                    HeadIndex = 1
                });
                await _unitOfWork.SaveChangesAsync();

                var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
                await groupRelationRepository.InsertAsync(new GroupRelation
                {
                    GroupId = response.GroupId,
                    UserId = userId,
                    Status = 0,
                    Grouping = "默认分组",
                    JoinTime = time
                });
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return (false, "本地数据库更新失败");
            }

            return (true, response.GroupId);
        }
        else
        {
            return (false, response?.Response.Message ?? "未知错误");
        }
    }

    public async Task<bool> UpdateGroupRelation(string userId, GroupRelationDto groupRelationDto)
    {
        var request = new UpdateGroupRelationRequest
        {
            UserId = userId,
            GroupId = groupRelationDto.Id,
            Grouping = groupRelationDto.Grouping,
            Remark = groupRelationDto.Remark ?? string.Empty,
            CantDisturb = groupRelationDto.CantDisturb,
            IsTop = groupRelationDto.IsTop,
            NickName = groupRelationDto.NickName ?? string.Empty
        };

        var response = await _messageHelper.SendMessageWithResponse<UpdateGroupRelation>(request);
        if (response is { Response: { State: true } })
        {
            var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
            var entity = await groupRelationRepository.GetFirstOrDefaultAsync(
                predicate: d => d.UserId.Equals(userId) && d.GroupId.Equals(groupRelationDto.Id),
                disableTracking: false
            );

            if (entity != null)
            {
                entity.Remark = groupRelationDto.Remark;
                entity.Grouping = groupRelationDto.Grouping;
                entity.CantDisturb = groupRelationDto.CantDisturb;
                entity.IsTop = groupRelationDto.IsTop;
                entity.NickName = groupRelationDto.NickName;
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                // ignored
            }
        }

        return false;
    }

    /// <summary>
    /// 更新群聊信息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupDto"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> UpdateGroup(string userId, GroupDto groupDto)
    {
        var request = new UpdateGroupMessageRequest
        {
            UserId = userId,
            GroupId = groupDto.Id,
            Name = groupDto.Name,
            Description = groupDto.Description ?? string.Empty,
            HeadIndex = groupDto.HeadIndex
        };

        await _messageHelper.SendMessage(request);
        return true;
    }

    /// <summary>
    /// 发送加入群聊请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<(bool, string)> JoinGroupRequest(string userId, string groupId)
    {
        var joinGroupRequest = new JoinGroupRequestFromClient
        {
            UserId = userId,
            GroupId = groupId,
            Message = ""
        };

        var response =
            await _messageHelper.SendMessageWithResponse<JoinGroupRequestResponseFromServer>(joinGroupRequest);

        if (response is { Response: { State: true } })
        {
            // 如果请求成功,数据库中保存此请求信息
            var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
            var groupRequest = new GroupRequest
            {
                UserFromId = userId,
                GroupId = groupId,
                RequestId = response.RequestId,
                RequestTime = DateTime.Parse(response.Time),
                Message = "",
                IsSolved = false
            };
            groupRequestRepository.Update(groupRequest);
            await _unitOfWork.SaveChangesAsync();

            // UI更新
            var userManager = _scopedProvider.Resolve<IUserManager>();
            var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
            var groupRequestDto = _mapper.Map<GroupRequestDto>(groupRequest);
            groupRequestDto.GroupDto = await userDtoManager.GetGroupDto(userId, groupId);
            userManager.GroupRequests?.Insert(0, groupRequestDto);
        }

        return (response?.Response.State ?? false, response?.Response?.Message ?? "未知错误");
    }

    /// <summary>
    /// 发送申请加入群聊的回应
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="requestId"></param>
    /// <param name="accept"></param>
    /// <returns></returns>
    public async Task<(bool, string)> JoinGroupResponse(string userId, int requestId, bool accept)
    {
        var joinGroupResponse = new JoinGroupResponseFromClient
        {
            Accept = accept,
            RequestId = requestId,
            UserId = userId
        };

        var response =
            await _messageHelper.SendMessageWithResponse<JoinGroupResponseResponseFromServer>(joinGroupResponse);

        return (response?.Response.State ?? false, response?.Response?.Message ?? "未知错误");
    }

    public async Task<Bitmap> GetHeadImage(int headIndex)
    {
        var fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();
        var bytes = await fileOperateHelper.GetGroupFile("HeadImage", $"{headIndex}.png");
        if (bytes != null)
        {
            Bitmap bitmap;
            using (var stream = new MemoryStream(bytes))
            {
                // 从流加载Bitmap
                bitmap = new Bitmap(stream);
            }

            Array.Clear(bytes);
            return bitmap;
        }

        return null;
    }

    public Task<Dictionary<int, Bitmap>> GetHeadImages()
    {
        throw new NotImplementedException();
    }
}