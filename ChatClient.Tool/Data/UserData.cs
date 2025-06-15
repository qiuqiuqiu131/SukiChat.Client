using System.Collections.ObjectModel;
using Avalonia.Collections;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Data;

public class UserData : IDisposable
{
    public UserDetailDto? UserDetail { get; set; }

    /// <summary>
    /// 接受到的好友请求
    /// </summary>
    public AvaloniaList<FriendReceiveDto>? FriendReceives { get; set; }

    /// <summary>
    /// 发送的好友请求
    /// </summary>
    public AvaloniaList<FriendRequestDto>? FriendRequests { get; set; }

    /// <summary>
    /// 好友删除消息
    /// </summary>
    public AvaloniaList<FriendDeleteDto>? FriendDeletes { get; set; }

    /// <summary>
    /// 好友列表
    /// </summary>
    public AvaloniaList<GroupFriendDto>? GroupFriends { get; set; }

    /// <summary>
    /// 好友聊天记录
    /// </summary>
    public AvaloniaList<FriendChatDto>? FriendChats { get; set; }

    /// <summary>
    /// 群聊天记录
    /// </summary>
    public AvaloniaList<GroupChatDto>? GroupChats { get; set; }

    /// <summary>
    /// 群聊列表
    /// </summary>
    public AvaloniaList<GroupGroupDto>? GroupGroups { get; set; }

    /// <summary>
    /// 加入群聊请求
    /// </summary>
    public AvaloniaList<GroupRequestDto>? GroupRequests { get; set; }

    /// <summary>
    /// 群聊删除消息
    /// </summary>
    public AvaloniaList<GroupDeleteDto>? GroupDeletes { get; set; }

    /// <summary>
    /// 申请加入群聊请求
    /// </summary>
    public AvaloniaList<GroupReceivedDto>? GroupReceiveds { get; set; }

    public void Dispose()
    {
        UserDetail = null;

        foreach (var friendReceive in FriendReceives!)
            friendReceive.Dispose();
        FriendReceives.Clear();

        foreach (var friendRequest in FriendRequests!)
            friendRequest.Dispose();
        FriendRequests.Clear();

        foreach (var groupFriend in GroupFriends!)
            groupFriend.Dispose();
        GroupFriends.Clear();

        foreach (var friendDelete in FriendDeletes!)
            friendDelete.Dispose();
        FriendDeletes.Clear();

        foreach (var friendChatDto in FriendChats!)
            friendChatDto.Dispose();
        FriendChats.Clear();

        foreach (var groupChatDto in GroupChats!)
            groupChatDto.Dispose();
        GroupChats.Clear();

        foreach (var groupGroupDto in GroupGroups!)
            groupGroupDto.Dispose();
        GroupGroups.Clear();

        foreach (var groupReceived in GroupReceiveds!)
            groupReceived.Dispose();
        GroupReceiveds.Clear();

        foreach (var groupDelete in GroupDeletes!)
            groupDelete.Dispose();
        GroupDeletes.Clear();

        foreach (var groupRequest in GroupRequests!)
            groupRequest.Dispose();
        GroupRequests.Clear();

        FriendReceives = null;
        FriendRequests = null;
        GroupFriends = null;
        FriendChats = null;
        FriendDeletes = null;
        GroupChats = null;
        GroupGroups = null;
        GroupRequests = null;
        GroupReceiveds = null;
        GroupDeletes = null;

        UserDetail = null;
    }
}