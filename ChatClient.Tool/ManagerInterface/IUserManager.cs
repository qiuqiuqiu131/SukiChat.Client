using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.ManagerInterface;

public interface IUserManager
{
    // 是否登录
    public bool IsLogin { get; }

    // 登录用户信息
    public UserDto? User { get; }
    public AvaloniaList<FriendReceiveDto>? FriendReceives { get; }
    public AvaloniaList<GroupFriendDto>? GroupFriends { get; }
    public AvaloniaList<FriendChatDto>? FriendChats { get; }

    // 用户登录请求
    public Task<CommonResponse?> Login(string id, string password, bool isRemember = false);

    // 用户登出请求
    public Task<CommonResponse?> Logout();

    // 重置头像
    public Task<bool> ResetHead(Bitmap? bitmap);

    // 保存用户信息
    public Task SaveUser();

    public Task<FriendRelationDto> NewFriendRecieve(string friendId);
}