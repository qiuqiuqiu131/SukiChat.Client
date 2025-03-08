using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using System.Collections.Concurrent;
using ChatClient.Tool.Data.Group;

namespace ChatClient.BaseService.Manager;

public interface IUserDtoManager
{
    Task<UserDto?> GetUserDto(string id);
    Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);
    Task<GroupDto?> GetGroupDto(string userId, string groupId);
    Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId);
    void Clear();
}

public class UserDtoManager : IUserDtoManager
{
    // 使用 ConcurrentDictionary 存储用户数据，确保线程安全
    private readonly ConcurrentDictionary<string, UserDto> _userDtos = new();
    private readonly ConcurrentDictionary<string, FriendRelationDto> _friendRelationDtos = new();
    private readonly ConcurrentDictionary<string, GroupDto> _groupDtos = new();
    private readonly ConcurrentDictionary<string, GroupRelationDto> _groupRelationDtos = new();

    private readonly IContainerProvider _containerProvider;
    private readonly SemaphoreSlim _semaphore_1 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_2 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_3 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_4 = new(1, 1);

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

    /// <summary>
    /// 获取GroupDto
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task<GroupDto?> GetGroupDto(string userId, string groupId)
    {
        if (_groupDtos.TryGetValue(groupId, out var cachedGroup))
            return cachedGroup;
        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_3.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_groupDtos.TryGetValue(groupId, out cachedGroup))
            {
                return cachedGroup;
            }

            var groupService = _containerProvider.Resolve<IGroupService>();
            var group = await groupService.GetGroupDto(userId, groupId);
            if (group == null) return null;
            var memberIds = await groupService.GetGroupMemberIds(userId, groupId);
            if (memberIds != null)
            {
                foreach (var memberId in memberIds)
                {
                    // 注入群组成员信息
                    var memberDto = await groupService.GetGroupMemberDto(groupId, memberId);
                    if (memberDto != null)
                        group.GroupMembers.Add(memberDto);
                }
            }

            // 如果获取到用户信息，添加到缓存中
            _groupDtos.TryAdd(groupId, group);

            return group;
        }
        finally
        {
            _semaphore_3.Release();
        }
    }

    public async Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId)
    {
        if (_groupRelationDtos.TryGetValue(groupId, out var cachedGroupRelation))
            return cachedGroupRelation;
        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_4.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_groupRelationDtos.TryGetValue(groupId, out cachedGroupRelation))
            {
                return cachedGroupRelation;
            }

            var groupService = _containerProvider.Resolve<IGroupService>();
            var groupRelation = await groupService.GetGroupRelationDto(userId, groupId);

            // 如果获取到用户信息，添加到缓存中
            if (groupRelation != null)
            {
                groupRelation.GroupDto = await GetGroupDto(userId, groupId);
                _groupRelationDtos.TryAdd(groupId, groupRelation);
            }

            return groupRelation;
        }
        finally
        {
            _semaphore_4.Release();
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