using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.ServiceEfCore.RemoteService;

internal class UserRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IUserService userService)
    : BaseService(containerProvider), IUserRemoteService
{
    public async Task<List<UserDto>> GetRemoteUsersAsync(string userId)
    {
        List<UserMessage> result = new();

        // 构建请求对象
        var request = new GetUserListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 50
        };

        // 循环获取用户信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetUserListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Users);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        // 加载用户实体
        List<Task<UserDto>> tasks = [];
        foreach (var userMessage in result)
            tasks.Add(userService.GetUserDto(userMessage));
        await Task.WhenAll(tasks);

        // 数据库保存
        _ = SaveUsersToDB(result).ConfigureAwait(false);

        return tasks.Select(d => d.Result).ToList();
    }

    private async Task SaveUsersToDB(IEnumerable<UserMessage> userMessage)
    {
        try
        {
            var mapper = _scopedProvider.Resolve<IMapper>();
            var unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
            var userRepository = unitOfWork.GetRepository<User>();
            foreach (var message in userMessage)
            {
                var user = mapper.Map<User>(message);
                if (await userRepository.ExistsAsync(d => d.Id == user.Id))
                    userRepository.Update(user);
                else
                    await userRepository.InsertAsync(user);
            }

            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
        }
    }
}