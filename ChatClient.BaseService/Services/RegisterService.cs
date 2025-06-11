using ChatClient.BaseService.Helper;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 注册服务接口
/// </summary>
public interface IRegisterService
{
    /// <summary>
    /// 注册账号
    /// </summary>
    /// <param name="name"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<RegisteResponse?> Register(string name, string password);
}

internal class RegisterService : BaseService, IRegisterService
{
    private readonly IMessageHelper _messageManager;

    public RegisterService(IMessageHelper messageManager, IContainerProvider containerProvider)
        : base(containerProvider)
    {
        _messageManager = messageManager;
    }

    public async Task<RegisteResponse?> Register(string name, string password)
    {
        var message = new RegisteRequest
        {
            Name = name,
            Password = password
        };

        var response = await _messageManager.SendMessageWithResponse<RegisteResponse>(message);

        return response;
    }
}