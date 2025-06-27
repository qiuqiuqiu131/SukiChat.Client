using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

internal class RegisterService(IMessageHelper messageManager, IContainerProvider containerProvider)
    : BaseService(containerProvider), IRegisterService
{
    public async Task<RegisteResponse?> Register(string name, string password, string? phone, string? email)
    {
        var message = new RegisteRequest
        {
            Name = name,
            Password = password,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty
        };

        var response = await messageManager.SendMessageWithResponse<RegisteResponse>(message);

        return response;
    }
}