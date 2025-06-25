using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

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