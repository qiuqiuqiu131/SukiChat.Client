using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.ManagerInterface;

public interface IUserManager
{
    // 是否登录
    bool IsLogin { get; }

    MainWindowState WindowState { get; set; }

    // 用户状态
    string CurrentChatPage { get; set; }
    ContactState CurrentContactState { get; set; }

    // 登录用户信息
    UserDetailDto? User { get; }
    AvaloniaList<FriendReceiveDto>? FriendReceives { get; }
    AvaloniaList<FriendRequestDto>? FriendRequests { get; }
    AvaloniaList<FriendDeleteDto>? FriendDeletes { get; }
    AvaloniaList<GroupFriendDto>? GroupFriends { get; }
    AvaloniaList<FriendChatDto>? FriendChats { get; }
    AvaloniaList<GroupChatDto>? GroupChats { get; }
    AvaloniaList<GroupGroupDto>? GroupGroups { get; }
    AvaloniaList<GroupDeleteDto>? GroupDeletes { get; }
    AvaloniaList<GroupReceivedDto>? GroupReceiveds { get; }
    AvaloniaList<GroupRequestDto>? GroupRequests { get; }

    // 用户登录请求
    Task<CommonResponse?> Login(string id, string password, bool isRemember = false);

    // 用户登出请求
    Task<CommonResponse?> Logout();

    // 重置头像
    Task<bool> ResetHead(Bitmap? bitmap);

    // 保存用户信息
    Task SaveUser();

    Task<FriendRelationDto?> NewFriendReceive(string friendId);
    Task<GroupRelationDto?> NewGroupReceive(string groupId);
    Task<GroupMemberDto?> NewGroupMember(string groupId, string userId);

    Task DeleteFriend(string friendId, string groupName);
    Task DeleteGroup(string groupId, string groupName);
    Task RemoveMember(string groupId, string memberId);
}

public enum MainWindowState
{
    Show,
    Hide,
    Close
}