using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IGroupService
{
    Task<bool> IsMember(string userId, string groupId);
    Task<(bool, string)> CreateGroup(string userId, List<string> members);
    Task<bool> UpdateGroupRelation(string userId, GroupRelationDto groupRelationDto);
    Task<bool> UpdateGroup(string userId, GroupDto groupDto);

    Task<(bool, string)> JoinGroupRequest(string userId, string groupId, string message, string nickName,
        string grouping, string remark);

    Task<(bool, string)> JoinGroupResponse(string userId, int requestId, bool accept);
    Task<(bool, string)> QuitGroupRequest(string userId, string groupId);
    Task<(bool, string)> RemoveMemberRequest(string userId, string groupId, string memberId);
    Task<(bool, string)> DisbandGroupRequest(string userId, string groupId);
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

    public Task<bool> IsMember(string userId, string groupId)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        return groupRelationRepository.ExistsAsync(d => d.UserId.Equals(userId) && d.GroupId.Equals(groupId));
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
                // doNothing
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
            NickName = groupRelationDto.NickName ?? string.Empty,
            IsChatting = groupRelationDto.IsChatting
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
                entity.IsChatting = groupRelationDto.IsChatting;
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
    public async Task<(bool, string)> JoinGroupRequest(string userId, string groupId, string message, string nickName,
        string grouping, string remark)
    {
        var joinGroupRequest = new JoinGroupRequestFromClient
        {
            UserId = userId,
            GroupId = groupId,
            Message = message,
            Grouping = grouping,
            NickName = nickName,
            Remark = remark
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
                Grouping = grouping,
                NickName = nickName,
                Remark = remark,
                Message = message,
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

    /// <summary>
    /// 请求退出群聊
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<(bool, string)> QuitGroupRequest(string userId, string groupId)
    {
        var request = new QuitGroupRequest
        {
            UserId = userId,
            GroupId = groupId
        };

        var response = await _messageHelper.SendMessageWithResponse<QuitGroupMessage>(request);
        return (response is { Response: { State: true } }, response?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> RemoveMemberRequest(string userId, string groupId, string memberId)
    {
        var request = new RemoveMemberRequest
        {
            UserId = userId,
            GroupId = groupId,
            MemberId = memberId
        };

        var response = await _messageHelper.SendMessageWithResponse<RemoveMemberMessage>(request);
        return (response is { Response: { State: true } }, response?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> DisbandGroupRequest(string userId, string groupId)
    {
        var request = new DisbandGroupRequest
        {
            UserId = userId,
            GroupId = groupId
        };

        var response = await _messageHelper.SendMessageWithResponse<RemoveMemberMessage>(request);
        return (response is { Response: { State: true } }, response?.Response?.Message ?? string.Empty);
    }
}