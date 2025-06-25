using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.Services.ServiceSugar;

internal class LoginSugarService : ILoginService
{
    private readonly IMessageHelper _messageManager;
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly IMapper _mapper;


    public LoginSugarService(IMessageHelper messageManager,
        ISqlSugarClient sqlSugarClient,
        IMapper mapper)
    {
        _messageManager = messageManager;
        _mapper = mapper;
        _sqlSugarClient = sqlSugarClient;
    }

    public async Task<LoginResponse?> Login(string id, string password, bool isRemember = false)
    {
        var message = new LoginRequest
        {
            Id = id,
            Password = password
        };

        var response = await _messageManager.SendMessageWithResponse<LoginResponse>(message);
        return response;
    }

    public async Task LoginSuccess(UserDetailDto user)
    {
        // 如果登录成功，获取用户的信息并添加或更新到数据库
        // 同时如果选择记住密码，添加登录历史
        await AddUser(user);
        await AddLoginHistory(user.UserDto, user.Password);
    }

    public async Task<List<LoginUserItem>> LoginUsers()
    {
        // var entitys = await _sqlSugarClient.Queryable<User>()
        //     .InnerJoin<LoginHistory>((u, l) => u.Id == l.Id)
        //     .OrderByDescending((u, l) => l.LastLoginTime)
        //     .Select((u, l) => new LoginUserItem
        //     {
        //         ID = u.Id,
        //         HeadIndex = u.HeadIndex,
        //         LastLoginTime = l.LastLoginTime
        //     }).ToListAsync();
        // return entitys;

        // 分步查询，避免复杂的 Join 表达式
        var loginHistories = await _sqlSugarClient.Queryable<LoginHistory>()
            .OrderByDescending(l => l.LastLoginTime)
            .ToListAsync();

        if (!loginHistories.Any()) return new List<LoginUserItem>();

        var userIds = loginHistories.Select(l => l.Id).ToList();
        var users = await _sqlSugarClient.Queryable<User>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var userDict = users.ToDictionary(u => u.Id);

        var result = new List<LoginUserItem>();
        foreach (var history in loginHistories)
        {
            if (userDict.TryGetValue(history.Id, out var user))
            {
                result.Add(new LoginUserItem
                {
                    ID = user.Id,
                    HeadIndex = user.HeadIndex,
                    LastLoginTime = history.LastLoginTime
                });
            }
        }

        return result;
    }

    public async Task<CommonResponse?> Logout(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var message = new LogoutRequest { Id = userId };
        return await _messageManager.SendMessageWithResponse<CommonResponse>(message);
    }

    public async Task<string?> GetPassword(string id)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext(false);
        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var history = await repository.GetFirstAsync(whereExpression: d => d.Id.Equals(id));
        return history?.Password;
    }

    private async Task AddUser(UserDetailDto user)
    {
        User userData = _mapper.Map<User>(user);

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var repository = _unitOfWork.GetRepository<User>();
            var entity = await repository.GetFirstAsync(whereExpression: d => d.Id.Equals(user.Id));
            if (entity != null)
                await repository.InsertOrUpdateAsync(userData);

            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }
    }

    private async Task AddLoginHistory(UserDto user, string password)
    {
        LoginHistory loginHistory = new LoginHistory
        {
            Id = user.Id,
            Password = password,
            LastLoginTime = DateTime.Now
        };

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var repository = _unitOfWork.GetRepository<LoginHistory>();
            await repository.InsertOrUpdateAsync(loginHistory);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }
    }

    public async Task RemoveLoginUser(string userId)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var repository = _unitOfWork.GetRepository<LoginHistory>();
            var entity = await repository.GetFirstAsync(whereExpression: d => d.Id == userId);
            if (entity != null)
                await repository.DeleteAsync(entity);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine($"Error removing login user: {e.Message}");
        }
    }

    public async Task<DateTime> GetLastLoginTime(string userId)
    {
        var entity = await _sqlSugarClient.Queryable<LoginHistory>()
            .FirstAsync(d => d.Id == userId);
        return entity?.LastLoginTime ?? DateTime.MinValue;
    }
}