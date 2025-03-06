using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IGroupService
{
    Task<(bool, string)> CreateGroup(string userId, string groupName);
    Task<bool> JoinGroupRequest(string userId, string groupId);
    Task<bool> JoinGroupResponse(string userId, int requestId, bool accept);
    Task<GroupMessage> GetGroupMessage(string userId, string groupId);
    Task<GroupMembersMessage> GetGroupMemberMessage(string userId, string groupId);
}

public class GroupService : BaseService, IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageHelper _messageHelper;

    public GroupService(IContainerProvider containerProvider) : base(containerProvider)
    {
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
        _messageHelper = _scopedProvider.Resolve<IMessageHelper>();
    }

    /// <summary>
    /// 向服务器发送创建群组请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    /// <returns>bool:是否正确处理请求,string:正确返回组群ID，错误返回报错信息</returns>
    public async Task<(bool, string)> CreateGroup(string userId, string groupName)
    {
        var createGroupRequest = new CreateGroupRequest
        {
            UserId = userId,
            GroupName = groupName
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
                groupRepository.Update(new Group
                {
                    Id = response.GroupId,
                    CreateTime = time
                });
                var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
                groupRelationRepository.Update(new GroupRelation
                {
                    GroupId = response.GroupId,
                    UserId = userId,
                    Status = 0,
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

    public Task<GroupMessage> GetGroupMessage(string userId, string groupId)
    {
        var groupMessageRequest = new GroupMessageRequest
        {
            GroupId = groupId,
            UserId = userId
        };

        return _messageHelper.SendMessageWithResponse<GroupMessage>(groupMessageRequest)!;
    }

    public Task<GroupMembersMessage> GetGroupMemberMessage(string userId, string groupId)
    {
        var groupMemberRequest = new GroupMembersRequest
        {
            GroupId = groupId,
            UserId = userId
        };

        return _messageHelper.SendMessageWithResponse<GroupMembersMessage>(groupMemberRequest)!;
    }
}