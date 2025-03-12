using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.ManagerInterface;

public interface IUserManager
{
    // 是否登录
    public bool IsLogin { get; }

    // 登录用户信息
    public UserDto? User { get; }
    public AvaloniaList<FriendReceiveDto>? FriendReceives { get; }
    public AvaloniaList<FriendRequestDto>? FriendRequests { get; }
    public AvaloniaList<GroupFriendDto>? GroupFriends { get; }
    public AvaloniaList<FriendChatDto>? FriendChats { get; }
    public AvaloniaList<GroupChatDto>? GroupChats { get; }
    public AvaloniaList<GroupGroupDto>? GroupGroups { get; }
    public AvaloniaList<GroupReceivedDto>? GroupReceiveds { get; }
    public AvaloniaList<GroupRequestDto>? GroupRequests { get; }

    // 用户登录请求
    public Task<CommonResponse?> Login(string id, string password, bool isRemember = false);

    // 用户登出请求
    public Task<CommonResponse?> Logout();

    // 重置头像
    public Task<bool> ResetHead(Bitmap? bitmap);

    // 保存用户信息
    public Task SaveUser();

    public Task<FriendRelationDto?> NewFriendReceive(string friendId);
    public Task<GroupRelationDto?> NewGroupReceive(string groupId);
    public Task<GroupMemberDto?> NewGroupMember(string groupId, string userId);
}