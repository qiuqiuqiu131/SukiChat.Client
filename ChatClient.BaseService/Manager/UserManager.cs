using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Media.CallManager;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Manager;

internal class UserManager : IUserManager
{
    public bool IsLogin { get; private set; } = false;

    public MainWindowState WindowState { get; set; } = MainWindowState.Show;

    #region UserData(用户所有的数据)

    // 主体数据
    public UserData? UserData { get; private set; }

    // 信息属性
    public UserDetailDto? User => UserData?.UserDetail;
    public AvaloniaList<FriendReceiveDto>? FriendReceives => UserData?.FriendReceives;
    public AvaloniaList<FriendRequestDto>? FriendRequests => UserData?.FriendRequests;
    public AvaloniaList<FriendDeleteDto>? FriendDeletes => UserData?.FriendDeletes;
    public AvaloniaList<GroupFriendDto>? GroupFriends => UserData?.GroupFriends;
    public AvaloniaList<FriendChatDto>? FriendChats => UserData?.FriendChats;
    public AvaloniaList<GroupChatDto>? GroupChats => UserData?.GroupChats;
    public AvaloniaList<GroupGroupDto>? GroupGroups => UserData?.GroupGroups;
    public AvaloniaList<GroupDeleteDto>? GroupDeletes => UserData?.GroupDeletes;
    public AvaloniaList<GroupReceivedDto>? GroupReceiveds => UserData?.GroupReceiveds;
    public AvaloniaList<GroupRequestDto>? GroupRequests => UserData?.GroupRequests;

    #endregion

    #region UserState

    public string CurrentChatPage { get; set; } = "聊天";

    public ContactState CurrentContactState { get; set; } = ContactState.None;

    #endregion

    private readonly IEventAggregator _eventAggregator;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IContainerProvider _containerProvider;

    public UserManager(
        IEventAggregator eventAggregator,
        IUserDtoManager userDtoManager,
        IContainerProvider containerProvider)
    {
        _eventAggregator = eventAggregator;
        _userDtoManager = userDtoManager;
        _containerProvider = containerProvider;
    }

    // 用户登录请求调用，返回登录的用户数据
    public async Task<CommonResponse?> Login(string id, string password, bool isRemember = false)
    {
        var loginService = _containerProvider.Resolve<ILoginService>();
        var response = await loginService.Login(id, password, isRemember);

        if (!(response is { Response: { State: true } })) return response?.Response;

        // 调用userService获取用户完整数据
        try
        {
            var _userService = _containerProvider.Resolve<IUserLoginService>();
            UserData = await _userService.GetUserFullData(id, password);
        }
        catch (Exception e)
        {
            await loginService.Logout(id);
            return new CommonResponse
            {
                State = false,
                Message = "加载错误，请重试"
            };
        }

        // 登录成功
        _ = loginService.LoginSuccess(UserData.UserDetail);

        IsLogin = true;
        // 登录成功后，程序启用全双工通信，开始监听消息。接收到消息后，由eventaggregator发布消息。 
        RegisterEvent(_eventAggregator);
        return response.Response;
    }

    /// <summary>
    /// 用户登出请求调用
    /// </summary>
    public async Task<CommonResponse?> Logout()
    {
        var loginService = _containerProvider.Resolve<ILoginService>();
        var response = await loginService.Logout(UserData?.UserDetail.Id);

        IsLogin = false;
        // 登出后，取消监听消息
        UnRegisterEvent(_eventAggregator);

        // 清空用户数据
        UserData?.Dispose();
        UserData = null;

        _userDtoManager.Clear();

        var imageManager = _containerProvider.Resolve<IImageManager>();
        imageManager.ClearCache();

        var callManager = _containerProvider.Resolve<ICallManager>();
        await callManager.RemoveCall();

        return response;
    }

    /// <summary>
    /// 添加头像
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public async Task<bool> ResetHead(Bitmap? bitmap)
    {
        if (bitmap == null || User == null) return false;

        var fileName = $"head_{User.UserDto.HeadCount}.png";
        bool result = false;
        await using (var stream = bitmap.BitmapToStream())
        {
            var _fileOperateHelper = _containerProvider.Resolve<IFileOperateHelper>();
            result = await _fileOperateHelper.UploadFile(User.Id, "HeadImage", fileName, stream, FileTarget.User);
        }

        if (!result) return false;

        User.UserDto.HeadCount++;
        User.UserDto.HeadIndex = User.UserDto.HeadCount - 1;
        var _imageManager = _containerProvider.Resolve<IImageManager>();
        var headImage = await _imageManager.GetFile(User.Id, "HeadImage", fileName, FileTarget.User);
        User.UserDto.HeadImage = headImage;

        await SaveUser();

        return true;
    }

    /// <summary>
    /// 在更改用户信息后保存用户，必须调用一下
    /// </summary>
    public async Task SaveUser()
    {
        var userService = _containerProvider.Resolve<IUserService>();
        await userService.SaveUser(User!);
    }

    #region NewDto

    /// <summary>
    /// 当添加了新朋友时，更新朋友列表
    /// </summary>
    /// <param name="friendId"></param>
    /// <param name="dto"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<FriendRelationDto?> NewFriendReceive(string friendId)
    {
        if (User == null) return null;

        var dto = await _userDtoManager.GetFriendRelationDto(User.Id, friendId);
        if (dto.UserDto == null)
            dto.UserDto = await _userDtoManager.GetUserDto(friendId);
        dto.UserDto.IsOnline = true;

        if (dto != null)
        {
            var groupFriend = GroupFriends?.FirstOrDefault(d => d.GroupName.Equals(dto.Grouping));
            if (groupFriend != null)
                groupFriend.Friends.Add(dto);
            else
            {
                if (GroupFriends == null)
                    UserData!.GroupFriends = new AvaloniaList<GroupFriendDto>();
                GroupFriends!.Add(new GroupFriendDto
                {
                    GroupName = dto.Grouping,
                    Friends = [dto]
                });
            }

            if (FriendChats == null)
                UserData!.FriendChats = new AvaloniaList<FriendChatDto>();
            FriendChats!.Add(new FriendChatDto
            {
                FriendRelatoinDto = dto,
                UserId = dto.Id
            });
        }

        return dto;
    }

    /// <summary>
    /// 接收到新的群组消息
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task<GroupRelationDto?> NewGroupReceive(string groupId)
    {
        if (User == null) return null;

        var dto = await _userDtoManager.GetGroupRelationDto(User!.Id, groupId);
        if (dto.GroupDto == null)
            dto.GroupDto = await _userDtoManager.GetGroupDto(User.Id, groupId);

        if (dto?.GroupDto != null)
            dto.GroupDto.IsEntered = true;

        if (dto != null)
        {
            var groupGroup = GroupGroups?.FirstOrDefault(d => d.GroupName.Equals(dto.Grouping));
            if (groupGroup != null)
                groupGroup.Groups.Add(dto);
            else
            {
                if (GroupGroups == null)
                    UserData!.GroupGroups = new AvaloniaList<GroupGroupDto>();
                GroupGroups!.Add(new GroupGroupDto
                {
                    GroupName = dto.Grouping,
                    Groups = [dto]
                });
            }

            if (GroupChats == null)
                UserData!.GroupChats = new AvaloniaList<GroupChatDto>();
            var chatDto = GroupChats!.FirstOrDefault(d => d.GroupRelationDto == dto);
            if (chatDto == null)
                GroupChats!.Add(new GroupChatDto
                {
                    GroupRelationDto = dto,
                    GroupId = groupId,
                });
        }

        return dto;
    }

    /// <summary>
    /// 接收到新的群组成员
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<GroupMemberDto?> NewGroupMember(string groupId, string userId)
    {
        if (User == null) return null;
        var dto = await _userDtoManager.GetGroupMemberDto(groupId, userId);
        if (dto != null)
        {
            var group = await _userDtoManager.GetGroupDto(User.Id, groupId);
            if (group != null)
            {
                group.GroupMembers.Add(dto);
            }
        }

        return dto;
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    public async Task DeleteFriend(string friendId, string groupName)
    {
        // 删除好友列表
        var friendGroup = GroupFriends!.FirstOrDefault(d => d.GroupName.Equals(groupName));
        if (friendGroup != null)
        {
            var friend = friendGroup.Friends.FirstOrDefault(d => d.Id.Equals(friendId));
            if (friend != null)
            {
                friendGroup.Friends.Remove(friend);
                await _userDtoManager.RemoveFriendRelationDto(friendId);
            }
        }

        // 删除聊天列表
        var friendChat = FriendChats!.FirstOrDefault(d => d.UserId.Equals(friendId));
        if (friendChat != null)
        {
            FriendChats!.Remove(friendChat);
            friendChat.Dispose();
        }

        var user = await _userDtoManager.GetUserDto(friendId);
        user.IsFriend = false;
        user.Remark = null;
    }

    public async Task DeleteGroup(string groupId, string groupName)
    {
        var groupGroup = GroupGroups!.FirstOrDefault(d => d.GroupName.Equals(groupName));
        if (groupGroup != null)
        {
            var group = groupGroup.Groups.FirstOrDefault(d => d.Id.Equals(groupId));
            if (group != null)
            {
                groupGroup.Groups.Remove(group);
                await _userDtoManager.RemoveGroupRelationDto(groupId);
            }
        }

        var groupChat = GroupChats!.FirstOrDefault(d => d.GroupId.Equals(groupId));
        if (groupChat != null)
        {
            GroupChats!.Remove(groupChat);
            groupChat.Dispose();
        }

        var groupDto = await _userDtoManager.GetGroupDto(User.Id, groupId);
        groupDto.IsEntered = false;
        groupDto.Remark = null;
    }

    public async Task RemoveMember(string groupId, string memberId)
    {
        var groupDto = await _userDtoManager.GetGroupDto(User!.Id, groupId);
        var memberDto = groupDto?.GroupMembers.FirstOrDefault(d => d.UserId.Equals(memberId));
        if (memberDto != null && groupDto != null)
        {
            groupDto.GroupMembers.Remove(memberDto);
            await _userDtoManager.RemoveGroupMemberDto(groupId, memberId);
        }
    }

    #endregion

    #region EventRegister

    // 用于存储所有的消息处理器
    private List<IMessageHandler>? handlers;

    private void RegisterEvent(IEventAggregator eventAggregator)
    {
        handlers = _containerProvider.Resolve<IEnumerable<IMessageHandler>>().ToList();
        foreach (var handler in handlers)
            handler.RegisterEvent(eventAggregator);
    }

    private void UnRegisterEvent(IEventAggregator eventAggregator)
    {
        foreach (var handler in handlers)
            handler.UnRegisterEvent(eventAggregator);
        handlers.Clear();
        handlers = null;
    }

    #endregion
}