using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using Google.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar.PackService;

public class GroupSugarPackService : Services.BaseService, IGroupPackService
{
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userManager;
    private readonly ISqlSugarClient _sqlSugarClient;

    public GroupSugarPackService(IContainerProvider containerProvider,
        IMapper mapper,
        IUserDtoManager userManager) : base(containerProvider)
    {
        _mapper = mapper;
        _userManager = userManager;
        _sqlSugarClient = _scopedProvider.Resolve<ISqlSugarClient>();
    }

    public async Task<AvaloniaList<GroupRelationDto>?> GetGroupRelationDtos(string userId)
    {
        var result = new AvaloniaList<GroupRelationDto>();

        var groupService = _scopedProvider.Resolve<IGroupGetService>();
        var groupIds = await groupService.GetGroupIds(userId);

        var tasks = groupIds.Select(async groupId =>
        {
            var groupRelationDto = await _userManager.GetGroupRelationDto(userId, groupId);
            return groupRelationDto;
        }).ToList();

        var groupRelationDtos = await Task.WhenAll(tasks);

        foreach (var groupRelationDto in groupRelationDtos)
        {
            if (groupRelationDto != null)
            {
                result.Add(groupRelationDto);
            }
        }

        return result;
    }

    public async Task<AvaloniaList<GroupRequestDto>?> GetGroupRequestDtos(string userId, DateTime lastDeleteTime)
    {
        var groupRequests = await _sqlSugarClient.Queryable<GroupRequest>()
            .Where(d => d.UserFromId == userId && (d.SolveTime ?? d.RequestTime) > lastDeleteTime)
            .OrderByDescending(d => d.SolveTime ?? d.RequestTime)
            .ToListAsync();

        if (groupRequests == null) return null;

        var groupRequestDtos = _mapper.Map<List<GroupRequestDto>>(groupRequests);
        foreach (var dto in groupRequestDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupRequestDto>(groupRequestDtos);
    }

    public async Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId, DateTime lastDeleteTime)
    {
        var groupIds = await _scopedProvider.Resolve<IGroupGetService>().GetGroupsOfUserManager(userId);

        var groupReceiveds = await _sqlSugarClient.Queryable<GroupReceived>()
            .Where(d => groupIds.Contains(d.GroupId) && (d.SolveTime ?? d.ReceiveTime) > lastDeleteTime)
            .OrderByDescending(d => d.SolveTime ?? d.ReceiveTime)
            .ToListAsync();

        if (groupReceiveds == null) return null;

        var groupReceivedDtos = _mapper.Map<List<GroupReceivedDto>>(groupReceiveds);
        foreach (var dto in groupReceivedDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                dto.UserDto = await _userManager.GetUserDto(dto.UserFromId);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupReceivedDto>(groupReceivedDtos);
    }

    public async Task<AvaloniaList<GroupDeleteDto>?> GetGroupDeleteDtos(string userId, DateTime lastDeleteTime)
    {
        var groupDeletes = await _sqlSugarClient.Queryable<GroupDelete>()
            .Where(d => d.MemberId == userId && d.DeleteTime > lastDeleteTime)
            .OrderByDescending(d => d.DeleteTime)
            .ToListAsync();

        if (groupDeletes == null) return null;

        var groupDeleteDtos = _mapper.Map<List<GroupDeleteDto>>(groupDeletes);
        foreach (var dto in groupDeleteDtos)
        {
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                dto.UserDto = await _userManager.GetUserDto(dto.OperateUserId);
            });
        }

        return new AvaloniaList<GroupDeleteDto>(groupDeleteDtos);
    }

    public async Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message)
    {
        if (!userId.Equals(message.UserIdTarget)) return false;

        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupRelation = _mapper.Map<GroupRelation>(message);
            var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
            await groupRelationRepository.InsertOrUpdateAsync(groupRelation);
            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();

            // 过滤有效消息并批量映射
            var validMessages = enterGroupMessages.Where(m => m.UserId.Equals(userId)).ToList();
            if (!validMessages.Any()) return true;

            var groupRelations = _mapper.Map<List<GroupRelation>>(validMessages);
            await groupRelationRepository.InsertOrUpdateAsync(groupRelations);
            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupDeleteMessageOperate(string userId, IMessage message)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupDelete = _mapper.Map<GroupDelete>(message);
            var groupDeleteRepository = unitOfWork.GetRepository<GroupDelete>();
            await groupDeleteRepository.InsertOrUpdateAsync(groupDelete);

            // 有可能是管理员发送，那么不删除GroupRelation
            if (groupDelete.MemberId.Equals(userId))
            {
                // 移除GroupRelation
                await _sqlSugarClient.Deleteable<GroupRelation>()
                    .Where(d => d.GroupId == groupDelete.GroupId && d.UserId == groupDelete.MemberId)
                    .ExecuteCommandAsync();
            }

            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupDeleteMessagesOperate(string userId,
        IEnumerable<GroupDeleteMessage> groupDeleteMessages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupDeleteRepository = unitOfWork.GetRepository<GroupDelete>();

            // 批量映射
            var groupDeletes = _mapper.Map<List<GroupDelete>>(groupDeleteMessages);
            await groupDeleteRepository.InsertOrUpdateAsync(groupDeletes);

            // // 顺序处理用户相关的群关系删除（避免并发冲突）
            var userRelatedMessages = groupDeleteMessages.Where(m => m.MemberId.Equals(userId)).ToList();
            foreach (var message in userRelatedMessages)
            {
                var deleteTime = DateTime.Parse(message.Time);

                await _sqlSugarClient.Deleteable<GroupRelation>()
                    .Where(d => d.GroupId == message.GroupId &&
                                d.UserId == message.MemberId &&
                                d.JoinTime < deleteTime)
                    .ExecuteCommandAsync();
            }

            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupRequestMessagesOperate(string userId, IEnumerable<GroupRequestMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var requestRepository = unitOfWork.GetRepository<GroupRequest>();

            // 批量映射
            var groupRequests = _mapper.Map<List<GroupRequest>>(messages);
            await requestRepository.InsertOrUpdateAsync(groupRequests);
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

    public async Task<bool> GroupReceivedMesssagesOperate(string userId, IEnumerable<GroupRequestMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var receiveRepository = unitOfWork.GetRepository<GroupReceived>();

            // 批量映射
            var groupReceiveds = _mapper.Map<List<GroupReceived>>(messages);
            await receiveRepository.InsertOrUpdateAsync(groupReceiveds);
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

    public async Task<bool> GroupRequestResponseMessageOperate(string userId, JoinGroupResponseFromServer message)
    {
        if (!message.Accept) return false;
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var requestRepository = unitOfWork.GetRepository<GroupRequest>();

            var groupRequest = await requestRepository.GetSingleAsync((gr) => gr.RequestId == message.RequestId);

            if (groupRequest == null) return false;

            var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
            var groupRelation = new GroupRelation
            {
                GroupId = groupRequest.GroupId,
                UserId = groupRequest.UserFromId,
                JoinTime = DateTime.Parse(message.Time),
                IsChatting = true,
                Grouping = groupRequest.Grouping,
                NickName = groupRequest.NickName,
                CantDisturb = false,
                IsTop = false,
                LastChatId = 0,
                Remark = groupRequest.Remark
            };

            await groupRelationRepository.InsertOrUpdateAsync(groupRelation);
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
}