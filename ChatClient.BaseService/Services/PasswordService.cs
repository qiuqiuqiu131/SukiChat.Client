using ChatClient.BaseService.Helper;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

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
    /// 忘记密码，通过手机号或邮箱号重置密码
    /// </summary>
    /// <param name="id">当前用户ID</param>
    /// <param name="phoneNumber">电话号</param>
    /// <param name="emailNumber">邮箱号</param>
    /// <param name="newPassword">新密码</param>
    /// <returns></returns>
    Task<(bool, string)> ForgetPasswordAsync(string id, string phoneNumber, string emailNumber, string newPassword);
}

public class PasswordService : BaseService, IPasswordService
{
    private readonly IMessageHelper _messageHelper;
    private readonly IUnitOfWork _unitOfWork;

    public PasswordService(IContainerProvider containerProvider, IMessageHelper messageHelper) : base(containerProvider)
    {
        _messageHelper = messageHelper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<(bool, string)> ResetPasswordAsync(string id, string origionalPassword, string newPassword)
    {
        var message = new ResetPasswordRequest
        {
            Id = id,
            OrigionalPassword = origionalPassword,
            NewPassword = newPassword
        };
        var result = await _messageHelper.SendMessageWithResponse<ResetPasswordResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, newPassword);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> UpdatePhoneNumberAsync(string id, string password, string PhoneNumber)
    {
        var message = new UpdatePhoneNumberRequest
        {
            UserId = id,
            Password = password,
            PhoneNumber = PhoneNumber
        };
        var result = await _messageHelper.SendMessageWithResponse<UpdatePhoneNumberResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, PhoneNumber);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> UpdateEmailAsync(string id, string password, string EmailNumber)
    {
        var message = new UpdateEmailNumberRequest
        {
            UserId = id,
            Password = password,
            EmailNumber = EmailNumber
        };

        var result = await _messageHelper.SendMessageWithResponse<UpdateEmailNumberResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, EmailNumber);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }

    public async Task<(bool, string)> ForgetPasswordAsync(string? id, string? phoneNumber, string? emailNumber,
        string newPassword)
    {
        var message = new ForgetPasswordRequest
        {
            Id = id ?? "",
            PhoneNumber = phoneNumber ?? "",
            EmailNumber = emailNumber ?? "",
            Password = newPassword
        };

        var result = await _messageHelper.SendMessageWithResponse<ForgetPasswordResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, result.UserId);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }
}