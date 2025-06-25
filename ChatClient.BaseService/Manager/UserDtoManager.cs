using ChatClient.Tool.Data;
using System.Collections.Concurrent;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;

namespace ChatClient.BaseService.Manager;

public interface IUserDtoManager
{
    Task AddUserDtos(List<UserDto> userDtos);
    Task AddGroupDtos(List<GroupDto> groupDtos);

    Task<UserDto?> GetUserDto(string id);
    Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);
    Task<GroupDto?> GetGroupDto(string userId, string groupId, bool loadMembers = true);
    Task<GroupRelationDto?> GetGroupRelationDto(string userId, string groupId);
    Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId);

    Task RemoveUserDto(string id);
    Task RemoveFriendRelationDto(string friendId);
    Task RemoveGroupDto(string groupId);
    Task RemoveGroupRelationDto(string groupId);
    Task RemoveGroupMemberDto(string groupId, string memberId);

    void Clear();
}

internal class UserDtoManager : IUserDtoManager
{
    // 使用 ConcurrentDictionary 存储用户数据，确保线程安全
    private readonly ConcurrentDictionary<string, UserDto> _userDtos = new();
    private readonly ConcurrentDictionary<string, FriendRelationDto> _friendRelationDtos = new();
    private readonly ConcurrentDictionary<string, GroupDto> _groupDtos = new();
    private readonly ConcurrentDictionary<string, GroupRelationDto> _groupRelationDtos = new();
    private readonly ConcurrentDictionary<string, GroupMemberDto> _groupMemberDtos = new();

    private readonly IContainerProvider _containerProvider;
    private readonly SemaphoreSlim _semaphore_1 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_2 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_3 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_4 = new(1, 1);
    private readonly SemaphoreSlim _semaphore_5 = new(1, 1);

    public UserDtoManager(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
    }

    public async Task AddUserDtos(List<UserDto> userDtos)
    {
        if (userDtos.Count == 0) return;
        try
        {
            await _semaphore_1.WaitAsync();
            foreach (var userDto in userDtos)
                _userDtos.TryAdd(userDto.Id, userDto);
        }
        finally
        {
            _semaphore_1.Release();
        }
    }

    private async Task AddGroupMemberDtos(List<GroupMemberDto> memberDtos)
    {
        if (memberDtos.Count == 0) return;
        try
        {
            await _semaphore_5.WaitAsync();
            foreach (var memberDto in memberDtos)
                _groupMemberDtos.TryAdd(memberDto.GroupId + memberDto.UserId, memberDto);
        }
        finally
        {
            _semaphore_5.Release();
        }
    }

    public async Task AddGroupDtos(List<GroupDto> groupDtos)
    {
        if (groupDtos.Count == 0) return;
        try
        {
            await _semaphore_3.WaitAsync();
            await _semaphore_5.WaitAsync();
            foreach (var groupDto in groupDtos)
            {
                _groupDtos.TryAdd(groupDto.Id, groupDto);
                foreach (var groupMember in groupDto.GroupMembers)
                {
                    string key = groupMember.GroupId + groupMember.UserId;
                    _groupMemberDtos.TryAdd(key, groupMember);
                }
            }
        }
        finally
        {
            _semaphore_3.Release();
            _semaphore_5.Release();
        }
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
            return cachedUser;

        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_1.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_userDtos.TryGetValue(id, out cachedUser))
                return cachedUser;

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
                return cachedFriend;

            var friendService = _containerProvider.Resolve<IFriendService>();
            var friend = await friendService.GetFriendRelationDto(userId, friendId);

            // 如果获取到用户信息，添加到缓存中
            if (friend != null)
            {
                _ = Task.Run(async () =>
                {
                    friend.UserDto = await GetUserDto(friendId);
                    if (friend.UserDto != null)
                    {
                        friend.UserDto.IsFriend = true;
                        friend.UserDto.Remark = friend.Remark;
                    }
                });
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
    public async Task<GroupDto?> GetGroupDto(string userId, string groupId, bool loadMembers = true)
    {
        if (_groupDtos.TryGetValue(groupId, out var cachedGroup))
        {
            // 加载群成员信息
            if (cachedGroup.GroupMembers.Count == 0 && loadMembers)
            {
                var groupRemoteService = _containerProvider.Resolve<IGroupRemoteService>();
                var memberLists = await groupRemoteService.GetRemoteGroupMembers(userId, groupId);
                await AddGroupMemberDtos(memberLists);
                cachedGroup.GroupMembers.AddRange(memberLists);
            }

            return cachedGroup;
        }

        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_3.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_groupDtos.TryGetValue(groupId, out cachedGroup))
                return cachedGroup;

            var groupService = _containerProvider.Resolve<IGroupGetService>();
            var group = await groupService.GetGroupDto(userId, groupId);

            if (group != null && loadMembers)
            {
                var groupRemoteService = _containerProvider.Resolve<IGroupRemoteService>();
                var memberLists = await groupRemoteService.GetRemoteGroupMembers(userId, groupId);
                await AddGroupMemberDtos(memberLists);
                group.GroupMembers.AddRange(memberLists);

                // 如果获取到用户信息，添加到缓存中
                _groupDtos.TryAdd(groupId, group);
            }

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
                return cachedGroupRelation;

            var groupService = _containerProvider.Resolve<IGroupGetService>();
            var groupRelation = await groupService.GetGroupRelationDto(userId, groupId);

            // 如果获取到用户信息，添加到缓存中
            if (groupRelation != null)
            {
                _ = Task.Run(async () =>
                {
                    groupRelation.GroupDto = await GetGroupDto(userId, groupId);
                    if (groupRelation.GroupDto != null)
                    {
                        groupRelation.GroupDto.IsEntered = true;
                        groupRelation.GroupDto.Remark = groupRelation.Remark;
                    }
                });
                //groupRelation.GroupDto = await GetGroupDto(userId, groupId);
                _groupRelationDtos.TryAdd(groupId, groupRelation);
            }

            return groupRelation;
        }
        finally
        {
            _semaphore_4.Release();
        }
    }

    public async Task<GroupMemberDto?> GetGroupMemberDto(string groupId, string memberId)
    {
        if (memberId.Equals("System")) return null;

        var key = groupId + memberId;
        if (_groupMemberDtos.TryGetValue(key, out var cachedGroupMember))
            return cachedGroupMember;
        try
        {
            // 使用信号量确保同一时间只有一个线程在请求相同的用户信息
            await _semaphore_5.WaitAsync();

            // 双重检查，防止在等待信号量时其他线程已经添加了数据
            if (_groupMemberDtos.TryGetValue(key, out cachedGroupMember))
                return cachedGroupMember;

            var groupService = _containerProvider.Resolve<IGroupGetService>();
            var groupMember = await groupService.GetGroupMemberDto(groupId, memberId);

            // 如果获取到用户信息，添加到缓存中
            if (groupMember != null)
            {
                _groupMemberDtos.TryAdd(key, groupMember);
            }

            return groupMember;
        }
        finally
        {
            _semaphore_5.Release();
        }
    }

    public async Task RemoveUserDto(string id)
    {
        try
        {
            await _semaphore_1.WaitAsync();
            _userDtos.TryRemove(id, out var user);
            user?.Dispose();
        }
        finally
        {
            _semaphore_1.Release();
        }
    }

    public async Task RemoveFriendRelationDto(string friendId)
    {
        try
        {
            await _semaphore_2.WaitAsync();
            _friendRelationDtos.TryRemove(friendId, out var friendRelation);
            friendRelation?.Dispose();
        }
        finally
        {
            _semaphore_2.Release();
        }
    }

    public async Task RemoveGroupDto(string groupId)
    {
        try
        {
            await _semaphore_3.WaitAsync();
            _groupDtos.TryRemove(groupId, out var group);
            foreach (var member in group.GroupMembers)
                await RemoveGroupMemberDto(groupId, member.UserId);
            group?.Dispose();
        }
        finally
        {
            _semaphore_3.Release();
        }
    }

    public async Task RemoveGroupRelationDto(string groupId)
    {
        try
        {
            await _semaphore_4.WaitAsync();
            _groupRelationDtos.TryRemove(groupId, out var groupRelation);
            groupRelation?.Dispose();
        }
        finally
        {
            _semaphore_4.Release();
        }
    }

    public async Task RemoveGroupMemberDto(string groupId, string memberId)
    {
        try
        {
            await _semaphore_5.WaitAsync();
            _groupMemberDtos.TryRemove(groupId + memberId, out var groupMember);
            groupMember?.Dispose();
        }
        finally
        {
            _semaphore_5.Release();
        }
    }

    public void Clear()
    {
        foreach (var friendRelationDto in _friendRelationDtos.Values)
            friendRelationDto.Dispose();
        _friendRelationDtos.Clear();

        foreach (var userDto in _userDtos.Values)
            userDto.Dispose();
        _userDtos.Clear();

        foreach (var groupDto in _groupDtos.Values)
            groupDto.Dispose();
        _groupDtos.Clear();

        foreach (var groupRelationDto in _groupRelationDtos.Values)
            groupRelationDto.Dispose();
        _groupRelationDtos.Clear();

        foreach (var groupMemberDto in _groupMemberDtos.Values)
            groupMemberDto.Dispose();
        _groupMemberDtos.Clear();
    }

    // 析构函数中释放信号量
    ~UserDtoManager()
    {
        _semaphore_1.Dispose();
        _semaphore_2.Dispose();
        _semaphore_3.Dispose();
        _semaphore_4.Dispose();
        _semaphore_5.Dispose();
        Clear();
    }
}