using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 登录服务接口
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="password">密码</param>
    /// <param name="isRemember">是否记住密码</param>
    /// <returns></returns>
    Task<LoginResponse?> Login(string id, string password, bool isRemember = false);

    /// <summary>
    /// 用户登出，退出账号后调用
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<CommonResponse?> Logout(string? userId);

    /// <summary>
    /// 从本地数据库的历史登录记录中获取对应用户的密码
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns></returns>
    Task<string?> GetPassword(string id);

    /// <summary>
    /// 登录成功后触发，更新用户信息和登录历史
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task LoginSuccess(UserDetailDto user);

    /// <summary>
    /// 获取本地历史登录用户列表
    /// </summary>
    /// <returns></returns>
    Task<List<LoginUserItem>> LoginUsers();

    /// <summary>
    /// 从本地历史记录中移除对应的用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task RemoveLoginUser(string userId);
}

internal class LoginService : BaseService, ILoginService
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
}