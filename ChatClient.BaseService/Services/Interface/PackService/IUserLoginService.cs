using ChatClient.Tool.Data;

namespace ChatClient.BaseService.Services.Interface.PackService;

public interface IUserLoginService
{
    /// <summary>
    /// 登录后加载用户完整信息，并处理离线时未处理的消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public Task<UserData> GetUserFullData(string userId, string password);
}