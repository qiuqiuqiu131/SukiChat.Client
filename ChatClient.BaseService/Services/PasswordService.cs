using ChatClient.BaseService.Helper;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IPasswordService
{
    Task<(bool, string)> ResetPasswordAsync(string id, string origionalPassword, string newPassword);

    Task<(bool, string)> UpdatePhoneNumberAsync(string id, string password, string phoneNumber);

    Task<(bool, string)> UpdateEmailAsync(string id, string password, string emailNumber);

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