using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.Services.ServiceSugar.RemoteService;

public class GroupSugarRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IGroupGetService groupService,
    IUserService userService) : IGroupRemoteService
{
    public async Task<List<GroupDto>> GetRemoteGroups(string userId)
    {
        var result = new List<GroupMessage>();

        // 构建请求对象
        var request = new GroupMessageListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取群聊信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GroupMessageListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Groups);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        List<Task<GroupDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(groupService.GetGroupDto(userMessage));

        await Task.WhenAll(tasks);

        _ = SaveGroupsToDB(result).ConfigureAwait(false);

        return tasks.Select(d => d.Result).ToList();
    }

    private async Task SaveGroupsToDB(IEnumerable<GroupMessage> groupMessages)
    {
        var sqlSugarClient = containerProvider.Resolve<ISqlSugarClient>();
        var mapper = containerProvider.Resolve<IMapper>();

        using var _unitOfWork = sqlSugarClient.CreateContext();
        try
        {
            var groupRepository = _unitOfWork.GetRepository<Group>();
            var groups = mapper.Map<List<Group>>(groupMessages);
            await groupRepository.InsertOrUpdateAsync(groups);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }
    }

    public async Task<List<GroupMemberDto>> GetRemoteGroupMembers(string userId, string groupId)
    {
        var result = new List<GroupMemberMessage>();

        // 构建请求对象
        var request = new GroupMemberListRequest
        {
            UserId = userId,
            GroupId = groupId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取群成员信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GroupMemberListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Members);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        List<Task<GroupMemberDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(groupService.GetGroupMemberDto(userMessage, userService));

        await Task.WhenAll(tasks);

        _ = SaveGroupMembersToDB(result).ConfigureAwait(false);

        return tasks.Select(d => d.Result).ToList();
    }

    private async Task SaveGroupMembersToDB(IEnumerable<GroupMemberMessage> groupMemberMessages)
    {
        var sqlSugarClient = containerProvider.Resolve<ISqlSugarClient>();
        var mapper = containerProvider.Resolve<IMapper>();

        using var _unitOfWork = sqlSugarClient.CreateContext();
        try
        {
            var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
            var members = mapper.Map<List<GroupMember>>(groupMemberMessages);
            await groupMemberRepository.InsertOrUpdateAsync(members);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }
    }
}