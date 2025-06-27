using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.EfCore;

public class GroupService : Services.BaseService, IGroupService
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
            try
            {
                var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
                var entity = await groupRequestRepository.GetFirstOrDefaultAsync(
                    predicate: d => d.GroupId == groupRequest.GroupId,
                    orderBy: o => o.OrderByDescending(d => d.RequestTime),
                    disableTracking: true);
                if (entity != null)
                    groupRequest.Id = entity.Id;
                groupRequestRepository.Update(groupRequest);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // UI更新
            var userManager = _scopedProvider.Resolve<IUserManager>();
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
                var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
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
        var fileOperateHelper = _scopedProvider.Resolve<IFileOperateHelper>();
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
        try
        {
            // 如果请求成功,数据库中更改此请求信息
            var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
            var groupRequest =
                await groupRequestRepository.GetFirstOrDefaultAsync(predicate: x => x.RequestId == message.RequestId,
                    disableTracking: false);
            if (groupRequest != null)
            {
                groupRequest.IsAccept = message.Accept;
                groupRequest.IsSolved = true;
                groupRequest.SolveTime = DateTime.Parse(message.Time);
                groupRequest.AcceptByUserId = message.UserId;
            }
            else
            {
                var gresponse = new GroupRequest
                {
                    RequestId = message.RequestId,
                    RequestTime = DateTime.Now,
                    SolveTime = null,
                    IsAccept = message.Accept,
                    IsSolved = true,
                    AcceptByUserId = message.UserId
                };
                groupRequestRepository.Update(gresponse);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<GroupRequestDto?> GetJoinGroupResponseFromServer(string userId,
        JoinGroupResponseFromServer message)
    {
        try
        {
            // 更新groupRequest表
            var requestRepository = _unitOfWork.GetRepository<GroupRequest>();
            var result = await requestRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.RequestId.Equals(message.RequestId), disableTracking: false);
            if (result == null) return null;
            result.IsSolved = true;
            result.IsAccept = message.Accept;
            result.AcceptByUserId = message.UserIdFrom;
            result.SolveTime = DateTime.Parse(message.Time);
            await _unitOfWork.SaveChangesAsync();

            // 更新groupRelation表
            var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
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
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<GroupRequestDto>(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<bool> GetJoinGroupRequestFromServer(string userId, GroupReceivedDto dto)
    {
        try
        {
            var receive = _mapper.Map<GroupReceived>(dto);
            var receiveRepository = _unitOfWork.GetRepository<GroupReceived>();
            var entity = await receiveRepository.GetFirstOrDefaultAsync(
                predicate: d => d.RequestId == dto.RequestId,
                orderBy: o => o.OrderByDescending(d => d.RequestId),
                disableTracking: true);
            if (entity != null)
                receive.Id = entity.Id;
            receiveRepository.Update(receive);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}