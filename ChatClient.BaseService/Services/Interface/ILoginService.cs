using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

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

    /// <summary>
    /// 从本地获取用户上次登录的时间
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<DateTime> GetLastLoginTime(string userId);
}