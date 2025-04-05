using ChatClient.BaseService.Helper;
using ChatClient.DataBase.UnitOfWork;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IPasswordService
{
    Task<(bool, string)> ResetPasswordAsync(string id, string origionalPassword, string newPassword);
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
}