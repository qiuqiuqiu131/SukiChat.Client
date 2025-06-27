using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar.RemoteService;

public class UserSugarRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper,
    IUserService userService)
    : IUserRemoteService
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
        var sqlSugarClient = containerProvider.Resolve<ISqlSugarClient>();
        var mapper = containerProvider.Resolve<IMapper>();

        using var _unitOfWork = sqlSugarClient.CreateContext();
        try
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var users = mapper.Map<List<User>>(userMessage);
            await userRepository.InsertOrUpdateAsync(users);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }
    }
}