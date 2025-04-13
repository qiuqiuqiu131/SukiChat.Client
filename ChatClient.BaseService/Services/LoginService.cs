using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface ILoginService
{
    Task<LoginResponse?> Login(string id, string password, bool isRemember = false);
    Task<CommonResponse?> Logout(string? userId);
    Task<string?> GetPassword(string id);
    Task LoginSuccess(UserDetailDto user);
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

    /// <summary>
    /// 登录请求
    /// </summary>
    /// <param name="id"></param>
    /// <param name="password"></param>
    /// <returns></returns>
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

    public async Task<CommonResponse?> Logout(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var message = new LogoutRequest
        {
            Id = userId
        };
        return await _messageManager.SendMessageWithResponse<CommonResponse>(message);
    }

    public async Task<string?> GetPassword(string id)
    {
        var repository = _unitOfWork.GetRepository<LoginHistory>();
        var history = await repository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(id));
        return history?.Password;
    }

    /// <summary>
    /// 将从服务器获取的用户信息添加到数据库
    /// </summary>
    /// <param name="user"></param>
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

    /// <summary>
    /// 登录成功后添加登录历史
    /// </summary>
    /// <param name="user"></param>
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
}