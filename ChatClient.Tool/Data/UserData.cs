using System.Collections.ObjectModel;
using Avalonia.Collections;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Data;

public class UserData
{
    public UserDto UserDetail { get; set; }

    /// <summary>
    /// 接受到的好友请求
    /// </summary>
    public AvaloniaList<FriendReceiveDto> FriendReceives { get; set; }

    /// <summary>
    /// 发送的好友请求
    /// </summary>
    public AvaloniaList<FriendRequestDto> FriendRequests { get; set; }

    /// <summary>
    /// 好友列表
    /// </summary>
    public AvaloniaList<GroupFriendDto> GroupFriends { get; set; }

    /// <summary>
    /// 好友聊天记录
    /// </summary>
    public AvaloniaList<FriendChatDto> FriendChatDtos { get; set; }

    /// <summary>
    /// 群聊天记录
    /// </summary>
    public AvaloniaList<GroupChatDto> GroupChatDtos { get; set; }

    /// <summary>
    /// 群聊列表
    /// </summary>
    public AvaloniaList<GroupGroupDto> GroupGroupDtos { get; set; }

    /// <summary>
    /// 加入群聊请求
    /// </summary>
    public AvaloniaList<GroupRequestDto> GroupRequests { get; set; }

    /// <summary>
    /// 申请加入群聊请求
    /// </summary>
    public AvaloniaList<GroupReceivedDto> GroupReceiveds { get; set; }


    public void Clear()
    {
        FriendReceives.Clear();
        GroupFriends.Clear();
        FriendChatDtos.Clear();
        GroupChatDtos.Clear();
        GroupGroupDtos.Clear();

        FriendReceives = null;
        GroupFriends = null;
        FriendChatDtos = null;
        GroupChatDtos = null;

        UserDetail = null;
    }
}