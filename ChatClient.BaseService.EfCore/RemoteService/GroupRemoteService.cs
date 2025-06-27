using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.EfCore.RemoteService;

internal class GroupRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IGroupGetService groupService,
    IUserService userService)
    : Services.BaseService(containerProvider), IGroupRemoteService
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
        try
        {
            var _mapper = _scopedProvider.Resolve<IMapper>();
            var _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
            var groupRepository = _unitOfWork.GetRepository<Group>();
            foreach (var message in groupMessages)
            {
                var group = _mapper.Map<Group>(message);
                if (await groupRepository.ExistsAsync(d => d.Id == group.Id))
                    groupRepository.Update(group);
                else
                    await groupRepository.InsertAsync(group);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
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
        try
        {
            var _mapper = _scopedProvider.Resolve<IMapper>();
            var _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
            var groupMemberRepository = _unitOfWork.GetRepository<GroupMember>();
            foreach (var message in groupMemberMessages)
            {
                var groupMember = _mapper.Map<GroupMember>(message);
                if (await groupMemberRepository.ExistsAsync(d =>
                        d.UserId == groupMember.UserId && d.GroupId == groupMember.GroupId))
                    groupMemberRepository.Update(groupMember);
                else
                    await groupMemberRepository.InsertAsync(groupMember);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
        }
    }
}