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

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupRelation = _mapper.Map<GroupRelation>(message);
            var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();

            var entity = await groupRelationRepository.GetFirstAsync(d =>
                d.GroupId == message.Grouping && d.UserId == userId);

            if (entity != null)
                groupRelation.Id = entity.Id;

            await groupRelationRepository.InsertOrUpdateAsync(groupRelation);
            _unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();

            // 过滤有效消息并批量映射
            var validMessages = enterGroupMessages.Where(m => m.UserId.Equals(userId)).ToList();
            if (!validMessages.Any()) return true;

            var groupRelations = _mapper.Map<List<GroupRelation>>(validMessages);

            // 批量查询已存在的关系
            var relationKeys = groupRelations.Select(gr => new { gr.GroupId, gr.UserId }).ToList();
            var existingRelations = await groupRelationRepository.GetListAsync(d =>
                relationKeys.Any(k => d.GroupId == k.GroupId && d.UserId == k.UserId));

            // 设置已有记录的ID
            var existingDict = existingRelations.ToDictionary(x => new { x.GroupId, x.UserId }, x => x.Id);
            foreach (var relation in groupRelations)
            {
                var key = new { relation.GroupId, relation.UserId };
                if (existingDict.TryGetValue(key, out var id))
                    relation.Id = id;
            }

            await groupRelationRepository.InsertOrUpdateAsync(groupRelations);
            _unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupDeleteMessageOperate(string userId, IMessage message)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupDelete = _mapper.Map<GroupDelete>(message);
            var groupDeleteRepository = _unitOfWork.GetRepository<GroupDelete>();

            // 添加群成员删除消息
            var entity = await groupDeleteRepository.GetFirstAsync(d => d.DeleteId == groupDelete.DeleteId);
            if (entity != null)
                groupDelete.Id = entity.Id;
            await groupDeleteRepository.InsertOrUpdateAsync(groupDelete);

            // 有可能是管理员发送，那么不删除GroupRelation
            if (groupDelete.MemberId.Equals(userId))
            {
                // 移除GroupRelation
                await _sqlSugarClient.Deleteable<GroupRelation>()
                    .Where(d => d.GroupId == groupDelete.GroupId && d.UserId == groupDelete.MemberId)
                    .ExecuteCommandAsync();
            }

            _unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupDeleteMessagesOperate(string userId,
        IEnumerable<GroupDeleteMessage> groupDeleteMessages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var groupDeleteRepository = _unitOfWork.GetRepository<GroupDelete>();

            // 批量映射
            var groupDeletes = _mapper.Map<List<GroupDelete>>(groupDeleteMessages);

            // 批量查询已存在记录
            var deleteIds = groupDeletes.Select(x => x.DeleteId).ToList();
            var existingDeletes = await groupDeleteRepository.GetListAsync(d => deleteIds.Contains(d.DeleteId));

            // 设置已有记录的ID
            var existingDict = existingDeletes.ToDictionary(x => x.DeleteId, x => x.Id);
            foreach (var delete in groupDeletes)
            {
                if (existingDict.TryGetValue(delete.DeleteId, out var id))
                    delete.Id = id;
            }

            await groupDeleteRepository.InsertOrUpdateAsync(groupDeletes);

            // 顺序处理用户相关的群关系删除（避免并发冲突）
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

            _unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> GroupRequestMessagesOperate(string userId, IEnumerable<GroupRequestMessage> messages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var requestRepository = _unitOfWork.GetRepository<GroupRequest>();

            // 批量映射
            var groupRequests = _mapper.Map<List<GroupRequest>>(messages);

            // 批量查询已存在记录
            var requestIds = groupRequests.Select(x => x.RequestId).ToList();
            var existingRequests = await requestRepository.GetListAsync(d => requestIds.Contains(d.RequestId));

            // 设置已有记录的ID
            var existingDict = existingRequests.ToDictionary(x => x.RequestId, x => x.Id);
            foreach (var request in groupRequests)
            {
                if (existingDict.TryGetValue(request.RequestId, out var id))
                    request.Id = id;
            }

            await requestRepository.InsertOrUpdateAsync(groupRequests);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> GroupReceivedMesssagesOperate(string userId, IEnumerable<GroupRequestMessage> messages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var receiveRepository = _unitOfWork.GetRepository<GroupReceived>();

            // 批量映射
            var groupReceiveds = _mapper.Map<List<GroupReceived>>(messages);

            // 批量查询已存在记录
            var requestIds = groupReceiveds.Select(x => x.RequestId).ToList();
            var existingReceiveds = await receiveRepository.GetListAsync(d => requestIds.Contains(d.RequestId));

            // 设置已有记录的ID
            var existingDict = existingReceiveds.ToDictionary(x => x.RequestId, x => x.Id);
            foreach (var received in groupReceiveds)
            {
                if (existingDict.TryGetValue(received.RequestId, out var id))
                    received.Id = id;
            }

            await receiveRepository.InsertOrUpdateAsync(groupReceiveds);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}