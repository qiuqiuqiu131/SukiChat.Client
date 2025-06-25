using Avalonia.Media.Imaging;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 用户信息服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户当前使用的头像
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="headIndex"></param>
    /// <returns></returns>
    public Task<Bitmap?> GetHeadImage(string userId, int headIndex);

    /// <summary>
    /// 获取用户的所有历史头像
    /// </summary>
    /// <param name="User"></param>
    /// <returns></returns>
    public Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User);

    /// <summary>
    /// 获取用户信息，作为用户基础实体，用于组装其他关系实体
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isUpdate"></param>
    /// <returns></returns>
    public Task<UserDto?> GetUserDto(string id, bool isUpdate = false);

    /// <summary>
    /// 根据UserMessage获取用户信息
    /// </summary>
    /// <param name="userMessage"></param>
    /// <returns></returns>
    public Task<UserDto> GetUserDto(UserMessage userMessage);

    /// <summary>
    /// 获取用户详细信息(当前登录者)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<UserDetailDto?> GetUserDetailDto(string id, string password);

    /// <summary>
    /// 保存用户详细信息(当前登陆者)
    /// </summary>
    /// <param name="User"></param>
    /// <returns></returns>
    public Task<bool> SaveUser(UserDetailDto User);
}