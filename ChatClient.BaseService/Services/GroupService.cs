using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 群聊关系服务接口
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// 判断用户是否是群组成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群组ID</param>
    /// <returns></returns>
    Task<bool> IsMember(string userId, string groupId);

    /// <summary>
    /// 创建群聊
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="members">选择的好友列表，将会拉取这些好友组建群聊</param>
    /// <returns></returns>
    Task<(bool, string)> CreateGroup(string userId, List<string> members);

    /// <summary>
    /// 更新群聊关系信息，如置顶、备注、消息免打扰等
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupRelationDto">群聊关系实体</param>
    /// <returns></returns>
    Task<bool> UpdateGroupRelation(string userId, GroupRelationDto groupRelationDto);

    /// <summary>
    /// 更新群聊信息，如群名、群描述等，只有群主有权修改群聊信息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupDto">群聊实体</param>
    /// <returns></returns>
    Task<bool> UpdateGroup(string userId, GroupDto groupDto);

    /// <summary>
    /// 发送加入群聊请求
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="message">请求消息</param>
    /// <param name="nickName">群聊昵称</param>
    /// <param name="grouping">群聊分组</param>
    /// <param name="remark">群聊备注</param>
    /// <returns></returns>
    Task<(bool, string)> JoinGroupRequest(string userId, string groupId, string message, string nickName,
        string grouping, string remark);

    /// <summary>
    /// 发送加入群聊的回应,用户的加群申请会被群主和群管理员接收到。
    /// 群主和群管理员有权审核用户的加群申请。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="requestId">加群请求ID</param>
    /// <param name="accept">是否同意</param>
    /// <returns></returns>
    Task<(bool, string)> JoinGroupResponse(string userId, int requestId, bool accept);

    /// <summary>
    /// 退出群聊请求
    /// </summary>
    /// <param name="userId">退群者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<(bool, string)> QuitGroupRequest(string userId, string groupId);

    /// <summary>
    /// 移除群成员，群主和群管理员有权移除群成员。
    /// 通知只能移除普通成员，不能移除群主和管理员。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="memberId">群成员ID</param>
    /// <returns></returns>
    Task<(bool, string)> RemoveMemberRequest(string userId, string groupId, string memberId);

    /// <summary>
    /// 解散群聊请求，只有群主有权解散群聊。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <returns></returns>
    Task<(bool, string)> DisbandGroupRequest(string userId, string groupId);

    /// <summary>
    /// 编辑群聊头像，只有群主有权编辑群聊头像。
    /// </summary>
    /// <param name="userId">处理者ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="bitmap">群头像</param>
    /// <returns></returns>
    Task<(bool, string)> EditGroupHead(string userId, string groupId, Bitmap bitmap);
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

    public async Task<bool> UpdateGroup(string userId, GroupDto groupDto)
    {
        var request = new UpdateGroupMessageRequest
        {
            UserId = userId,
            GroupId = groupDto.Id,
            Name = groupDto.Name,
            Description = groupDto.Description ?? string.Empty
        };

        await _messageHelper.SendMessage(request);
        return true;
    }

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

    public async Task<(bool, string)> EditGroupHead(string userId, string groupId, Bitmap bitmap)
    {
        var fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();
        var result1 = await fileOperateHelper.UploadFile(groupId, "HeadImage", $"{groupId}.png",
            bitmap.BitmapToByteArray(),
            FileTarget.Group);

        if (!result1) return (false, "上传头像失败");

        var request = new ResetHeadImageRequest
        {
            UserId = userId,
            GroupId = groupId
        };
        var result2 = await _messageHelper.SendMessageWithResponse<ResetHeadImageResponse>(request);
        return (result2?.Response.State ?? false, result2?.Response.Message ?? "");
    }
}