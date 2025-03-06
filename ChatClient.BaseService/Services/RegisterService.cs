using ChatClient.BaseService.Helper;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IRegisterService
{
    Task<CommonResponse?> Register(string name, string password);
}

internal class RegisterService : BaseService, IRegisterService
{
    private readonly IMessageHelper _messageManager;

    public RegisterService(IMessageHelper messageManager, IContainerProvider containerProvider)
        : base(containerProvider)
    {
        _messageManager = messageManager;
    }

    public async Task<CommonResponse?> Register(string name, string password)
    {
        var message = new RegisteRequest
        {
            Name = name,
            Password = password
        };

        var response = await _messageManager.SendMessageWithResponse<CommonResponse>(message);

        return response;
    }
}