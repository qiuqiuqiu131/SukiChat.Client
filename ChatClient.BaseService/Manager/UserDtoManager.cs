using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using System.Collections.Concurrent;

namespace ChatClient.BaseService.Manager;

public interface IUserDtoManager
{
    Task<UserDto?> GetUserDto(string id);
    Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);
    void Clear();
}

public class UserDtoManager : IUserDtoManager
{
    // 使用 ConcurrentDictionary 存储用户数据，确保线程安全
    private readonly ConcurrentDictionary<string, UserDto> _userDtos = new();
    private readonly ConcurrentDictionary<string, FriendRelationDto> _friendRelationDtos = new();

    private readonly IContainerProvider _containerProvider;
    private readonly SemaphoreSlim _semaphore_1 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_2 = new(1, 1);

    public UserDtoManager(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<UserDto?> GetUserDto(string id)
    {
        // 先尝试从缓存中获取
        if (_userDtos.TryGetValue(id, out var cachedUser))
        {
            return cachedUser;
        }

        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_1.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_userDtos.TryGetValue(id, out cachedUser))
            {
                return cachedUser;
            }

            var userService = _containerProvider.Resolve<IUserService>();
            var user = await userService.GetUserDto(id);

            // 如果获取到用户信息，添加到缓存中
            if (user != null)
            {
                _userDtos.TryAdd(id, user);
            }

            return user;
        }
        finally
        {
            _semaphore_1.Release();
        }
    }

    /// <summary>
    /// 获取FriendRelatoinDto
    /// </summary>
    public async Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId)
    {
        if (_friendRelationDtos.TryGetValue(friendId, out var cachedFriend))
            return cachedFriend;
        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_2.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_friendRelationDtos.TryGetValue(friendId, out cachedFriend))
            {
                return cachedFriend;
            }

            var friendService = _containerProvider.Resolve<IFriendService>();
            var friend = await friendService.GetFriendRelationDto(userId, friendId);

            // 如果获取到用户信息，添加到缓存中
            if (friend != null)
            {
                friend.UserDto = await GetUserDto(friendId);
                _friendRelationDtos.TryAdd(friendId, friend);
            }

            return friend;
        }
        finally
        {
            _semaphore_2.Release();
        }
    }

    public void Clear()
    {
        _friendRelationDtos.Clear();
        _userDtos.Clear();
    }

    // 析构函数中释放信号量
    ~UserDtoManager()
    {
        _semaphore_1.Dispose();
        _semaphore_2.Dispose();
    }
}