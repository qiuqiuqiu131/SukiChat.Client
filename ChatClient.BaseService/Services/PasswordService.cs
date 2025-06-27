using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public class PasswordService(IContainerProvider containerProvider, IMessageHelper messageHelper)
    : BaseService(containerProvider), IPasswordService
{
    public async Task<(bool, string)> ResetPasswordAsync(string id, string origionalPassword, string newPassword)
    {
        var message = new ResetPasswordRequest
        {
            Id = id,
            OrigionalPassword = origionalPassword,
            NewPassword = newPassword
        };
        var result = await messageHelper.SendMessageWithResponse<ResetPasswordResponse>(message);
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
        var result = await messageHelper.SendMessageWithResponse<UpdatePhoneNumberResponse>(message);
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

        var result = await messageHelper.SendMessageWithResponse<UpdateEmailNumberResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, EmailNumber);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }

    public Task<PasswordAuthenticateResponse?> ForgetPasswordConfirmAsync(string phoneNumber, string emailNumber)
    {
        var message = new PasswordAuthenticateRequest
        {
            Phone = phoneNumber,
            Email = emailNumber
        };

        return messageHelper.SendMessageWithResponse<PasswordAuthenticateResponse>(message);
    }

    public async Task<(bool, string)> ForgetPasswordResetAsync(string userId, string passKey, string newPassword)
    {
        var message = new ForgetPasswordRequest
        {
            Password = newPassword,
            UserId = userId,
            PassKey = passKey
        };

        var result = await messageHelper.SendMessageWithResponse<ForgetPasswordResponse>(message);
        if (result is { Response: { State: true } })
        {
            return (true, result.UserId);
        }

        return (false, result?.Response?.Message ?? string.Empty);
    }
}