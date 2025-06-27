using AutoMapper;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.EfCore;

internal class LoginService : Services.BaseService, ILoginService
{
    private readonly IMessageHelper _messageManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;


    public LoginService(IMessageHelper messageManager,
        IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
    {
        _messageManager = messageManager;
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
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
        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var logins = await repository.GetAllAsync();
        var ids = logins.Select(d => d.Id).ToList();
        var times = logins.ToDictionary(d => d.Id, d => d.LastLoginTime);

        var userRepository = _unitOfWork.GetRepository<User>();
        var entitys = await userRepository.GetAll(
                predicate: d => ids.Contains(d.Id))
            .Select(d => new LoginUserItem
                { ID = d.Id, HeadIndex = d.HeadIndex, LastLoginTime = times[d.Id] })
            .ToListAsync();

        return entitys.OrderByDescending(d => d.LastLoginTime).ToList();
    }

    public async Task<CommonResponse?> Logout(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var message = new LogoutRequest { Id = userId };
        return await _messageManager.SendMessageWithResponse<CommonResponse>(message);
    }

    public async Task<string?> GetPassword(string id)
    {
        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var history = await repository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(id));
        return history?.Password;
    }

    private async Task AddUser(UserDetailDto user)
    {
        User userData = _mapper.Map<User>(user);

        var repository = _unitOfWork.GetRepository<User>();
        var entity = await repository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id));
        if (entity != null)
        {
            try
            {
                repository.Update(userData);
            }
            catch (Exception e)
            {
                // 数据实体被占用
            }
        }
        else
            await repository.InsertAsync(userData);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task AddLoginHistory(UserDto user, string password)
    {
        LoginHistory loginHistory = new LoginHistory
        {
            Id = user.Id,
            Password = password,
            LastLoginTime = DateTime.Now
        };

        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var history = await repository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id));

        if (history != null)
        {
            try
            {
                repository.Update(loginHistory);
            }
            catch (Exception e)
            {
                // 数据实体被占用
            }
        }
        else
            await repository.InsertAsync(loginHistory);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveLoginUser(string userId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LoginHistory>();
            var entity = await repository.GetFirstOrDefaultAsync(predicate: d => d.Id == userId);
            if (entity != null)
                repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 处理异常，例如记录日志或显示错误消息
            Console.WriteLine($"Error removing login user: {e.Message}");
        }
    }

    public async Task<DateTime> GetLastLoginTime(string userId)
    {
        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var entity = await repository.GetFirstOrDefaultAsync(
            predicate: d => d.Id == userId,
            orderBy: d => d.OrderByDescending(o => o.LastLoginTime));
        return entity?.LastLoginTime ?? DateTime.MinValue;
    }
}