using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar;

public class GroupSugarService : IGroupService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly IContainerProvider _containerProvider;
    private readonly IMapper _mapper;
    private readonly IMessageHelper _messageHelper;

    public GroupSugarService(IContainerProvider containerProvider,
        IMessageHelper messageHelper,
        ISqlSugarClient sqlSugarClient,
        IMapper mapper)
    {
        _containerProvider = containerProvider;
        _mapper = mapper;
        _messageHelper = messageHelper;
        _sqlSugarClient = sqlSugarClient;
    }

    public Task<bool> IsMember(string userId, string groupId)
    {
        return _sqlSugarClient.Queryable<GroupRelation>()
            .Where(d => d.UserId == userId && d.GroupId == groupId)
            .AnyAsync();
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
            using var unitOfWork = _sqlSugarClient.CreateContext();
            try
            {
                var groupRepository = unitOfWork.GetRepository<Group>();
                await groupRepository.InsertOrUpdateAsync(new Group
                {
                    Id = response.GroupId,
                    Name = response.GroupName,
                    CreateTime = time,
                    HeadIndex = 1
                });

                var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
                await groupRelationRepository.InsertOrUpdateAsync(new GroupRelation
                {
                    GroupId = response.GroupId,
                    UserId = userId,
                    Status = 0,
                    Grouping = "默认分组",
                    JoinTime = time
                });

                unitOfWork.Commit();
            }
            catch (Exception e)
            {
                await unitOfWork.Tenant.RollbackTranAsync();
                Console.WriteLine(e);
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
            using var unitOfWork = _sqlSugarClient.CreateContext();
            try
            {
                var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
                var entity = await groupRelationRepository.GetFirstAsync(whereExpression: d =>
                    d.UserId == userId && d.GroupId == groupRelationDto.Id);
                if (entity != null)
                {
                    entity.Grouping = groupRelationDto.Grouping;
                    entity.Remark = groupRelationDto.Remark;
                    entity.CantDisturb = groupRelationDto.CantDisturb;
                    entity.IsTop = groupRelationDto.IsTop;
                    entity.NickName = groupRelationDto.NickName;
                    entity.IsChatting = groupRelationDto.IsChatting;
                    await groupRelationRepository.UpdateAsync(entity);
                }

                unitOfWork.Commit();
            }
            catch (Exception e)
            {
                await unitOfWork.Tenant.RollbackTranAsync();
                Console.WriteLine(e);
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
            using var unitOfWork = _sqlSugarClient.CreateContext();
            try
            {
                var groupRequestRepository = unitOfWork.GetRepository<GroupRequest>();
                await groupRequestRepository.UpdateAsync(groupRequest);
                unitOfWork.Commit();
            }
            catch (Exception e)
            {
                await unitOfWork.Tenant.RollbackTranAsync();
                Console.WriteLine(e);
            }

            // UI更新
            var userManager = _containerProvider.Resolve<IUserManager>();
            var dto = userManager.GroupRequests?.FirstOrDefault(d => d.RequestId == groupRequest.RequestId);
            if (dto != null)
            {
                dto.Message = groupRequest.Message;
                dto.IsSolved = false;

                userManager.GroupRequests?.Remove(dto);
                userManager.GroupRequests?.Insert(0, dto);
            }
            else
            {
                var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
                dto = _mapper.Map<GroupRequestDto>(groupRequest);
                dto.GroupDto = await userDtoManager.GetGroupDto(userId, groupId, false);

                userManager.GroupRequests?.Insert(0, dto);
            }
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

        var response = await _messageHelper.SendMessageWithResponse<DisbandGroupMessage>(request);
        return (response is { Response: { State: true } }, response?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> EditGroupHead(string userId, string groupId, Bitmap bitmap)
    {
        var fileOperateHelper = _containerProvider.Resolve<IFileOperateHelper>();
        var result1 = false;
        await using (var stream = bitmap.BitmapToStream())
            result1 = await fileOperateHelper.UploadFile(groupId, "HeadImage", $"{groupId}.png", stream,
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

    public async Task<bool> GetJoinGroupResponseResponseFromServer(string userId,
        JoinGroupResponseResponseFromServer message)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            // 如果请求成功,数据库中更改此请求信息
            var groupReceivedRepository = unitOfWork.GetRepository<GroupReceived>();
            var groupReceived =
                await groupReceivedRepository.GetFirstAsync(x => x.RequestId == message.RequestId);
            if (groupReceived != null)
            {
                groupReceived.IsAccept = message.Accept;
                groupReceived.IsSolved = true;
                groupReceived.SolveTime = DateTime.Parse(message.Time);
                groupReceived.AcceptByUserId = message.UserId;
                await groupReceivedRepository.UpdateAsync(groupReceived);
            }

            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<GroupRequestDto?> GetJoinGroupResponseFromServer(string userId,
        JoinGroupResponseFromServer message)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            // 更新groupRequest表
            var requestRepository = unitOfWork.GetRepository<GroupRequest>();
            var result = await requestRepository.GetFirstAsync(d =>
                d.RequestId == message.RequestId);
            if (result == null) return null;
            result.IsSolved = true;
            result.IsAccept = message.Accept;
            result.AcceptByUserId = message.UserIdFrom;
            result.SolveTime = DateTime.Parse(message.Time);
            await unitOfWork.Tenant.CommitTranAsync();

            // 更新groupRelation表
            var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
            if (message.Accept)
            {
                var groupRelation = new GroupRelation
                {
                    GroupId = result.GroupId,
                    UserId = result.UserFromId,
                    Status = 2,
                    JoinTime = DateTime.Now,
                    Grouping = result.Grouping,
                    NickName = result.NickName,
                    Remark = result.Remark
                };
                await groupRelationRepository.InsertAsync(groupRelation);
                unitOfWork.Commit();
            }

            return _mapper.Map<GroupRequestDto>(result);
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<bool> GetJoinGroupRequestFromServer(string userId, GroupReceivedDto dto)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var receive = _mapper.Map<GroupReceived>(dto);
            var receiveRepository = unitOfWork.GetRepository<GroupReceived>();
            await receiveRepository.InsertOrUpdateAsync(receive);

            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}