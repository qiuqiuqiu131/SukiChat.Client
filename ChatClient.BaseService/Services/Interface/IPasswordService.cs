using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 安全相关服务接口
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="id">当前用户ID</param>
    /// <param name="origionalPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    /// <returns></returns>
    Task<(bool, string)> ResetPasswordAsync(string id, string origionalPassword, string newPassword);

    /// <summary>
    /// 更新手机号
    /// </summary>
    /// <param name="id">当前用户ID</param>
    /// <param name="password">密码</param>
    /// <param name="phoneNumber">手机号</param>
    /// <returns></returns>
    Task<(bool, string)> UpdatePhoneNumberAsync(string id, string password, string phoneNumber);

    /// <summary>
    /// 更新邮箱号
    /// </summary>
    /// <param name="id">当前用户ID</param>
    /// <param name="password">密码</param>
    /// <param name="emailNumber">邮箱号</param>
    /// <returns></returns>
    Task<(bool, string)> UpdateEmailAsync(string id, string password, string emailNumber);

    /// <summary>
    /// 忘记密码，通过手机号或邮箱号认证身份状态
    /// </summary>
    /// <param name="id">当前用户ID</param>
    /// <param name="phoneNumber">电话号</param>
    /// <param name="emailNumber">邮箱号</param>
    /// <param name="newPassword">新密码</param>
    /// <returns></returns>
    Task<PasswordAuthenticateResponse?> ForgetPasswordConfirmAsync(string phoneNumber, string emailNumber);

    /// <summary>
    /// 忘记密码，重置密码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="passKey"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    Task<(bool, string)> ForgetPasswordResetAsync(string userId, string passKey, string newPassword);
}