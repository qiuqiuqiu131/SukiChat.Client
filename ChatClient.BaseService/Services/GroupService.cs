using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IGroupService
{
    Task<GroupDto?> GetGroupDto(string userId, string groupId);
    Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId);
    Task<(bool, string)> CreateGroup(string userId, List<string> members);
    Task<bool> JoinGroupRequest(string userId, string groupId);
    Task<bool> JoinGroupResponse(string userId, int requestId, bool accept);
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

    public async Task<GroupDto?> GetGroupDto(string userId, string groupId)
    {
        var groupRequest = new GroupMessageRequest { GroupId = groupId, UserId = userId };
        var groupMessage = await _messageHelper.SendMessageWithResponse<GroupMessage>(groupRequest);
        if (groupMessage == null) return null;

        var groupDto = _mapper.Map<GroupDto>(groupMessage);

        var group = _mapper.Map<Group>(groupDto);
        var groupRepository = _unitOfWork.GetRepository<Group>();
        groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();

        return groupDto;
    }

    public async Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId)
    {
        var groupMemberRequest = new GroupMemberRequest { GroupId = groupId, MemberId = memberId };
        var groupMemberMessage = await _messageHelper.SendMessageWithResponse<GroupMemberMessage>(groupMemberRequest)!;
        if (groupMemberMessage == null) return null;

        var groupMemberDto = _mapper.Map<GroupMemberDto>(groupMemberMessage);
        var userService = _scopedProvider.Resolve<IUserService>();
        groupMemberDto.HeadImage = await userService.GetHeadImage(groupMemberDto.UserId, groupMemberDto.HeadIndex);

        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        var groupRelation = _mapper.Map<GroupRelation>(groupMemberDto);
        var entity = await groupRelationRepository.GetFirstOrDefaultAsync(
            predicate: d => d.GroupId.Equals(groupId) && d.UserId.Equals(memberId));
        if (entity != null)
            groupRelation.Id = entity.Id;
        groupRelationRepository.Update(groupRelation);
        await _unitOfWork.SaveChangesAsync();

        return groupMemberDto;
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
                    HeadPath = "-1"
                });
                await _unitOfWork.SaveChangesAsync();

                var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
                var userService = _scopedProvider.Resolve<IUserService>();
                var user = await userService.GetUserDto(userId);
                await groupRelationRepository.InsertAsync(new GroupRelation
                {
                    GroupId = response.GroupId,
                    UserId = userId,
                    Status = 0,
                    Grouping = "默认分组",
                    JoinTime = time,
                    HeadIndex = user.HeadIndex
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


    /// <summary>
    /// 发送加入群聊请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> JoinGroupRequest(string userId, string groupId)
    {
        var joinGroupRequest = new JoinGroupRequestFromClient
        {
            UserId = userId,
            GroupId = groupId,
        };

        var response =
            await _messageHelper.SendMessageWithResponse<JoinGroupRequestResponseFromServer>(joinGroupRequest);

        if (response is { Response: { State: true } })
        {
            // 如果请求成功,数据库中保存此请求信息
            var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
            groupRequestRepository.Update(new GroupRequest
            {
                UserFromId = userId,
                GroupId = groupId,
                RequestId = response.RequestId,
                RequestTime = DateTime.Parse(response.Time),
                IsSolved = false
            });
            await _unitOfWork.SaveChangesAsync();
        }

        return response?.Response.State ?? false;
    }

    /// <summary>
    /// 发送申请加入群聊的回应
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="requestId"></param>
    /// <param name="accept"></param>
    /// <returns></returns>
    public async Task<bool> JoinGroupResponse(string userId, int requestId, bool accept)
    {
        var joinGroupResponse = new JoinGroupResponseFromClient
        {
            Accept = accept,
            RequestId = requestId,
            UserId = userId
        };

        var response =
            await _messageHelper.SendMessageWithResponse<JoinGroupResponseResponseFromServer>(joinGroupResponse);

        if (response is { Response: { State: true } })
        {
            // 如果请求成功,数据库中更改此请求信息
            var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
            var groupRequest =
                await groupRequestRepository.GetFirstOrDefaultAsync(predicate: x => x.RequestId == requestId,
                    disableTracking: false);
            if (groupRequest != null)
            {
                groupRequest.IsAccept = response.Accept;
                groupRequest.IsSolved = true;
                groupRequest.SolveTime = DateTime.Parse(response.Time);
                groupRequest.AcceptByUserId = response.UserId;
            }
            else
            {
                var gresponse = new GroupRequest
                {
                    RequestId = response.RequestId,
                    RequestTime = DateTime.Now,
                    SolveTime = DateTime.Parse(response.Time),
                    IsAccept = response.Accept,
                    IsSolved = true,
                    AcceptByUserId = response.UserId
                };
                groupRequestRepository.Update(gresponse);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                return false;
            }

            return true;
        }

        return false;
    }
}