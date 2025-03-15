using System.Collections.ObjectModel;
using Avalonia.Collections;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Data;

public class UserData : IDisposable
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

    public void Dispose()
    {
        UserDetail = null;

        foreach (var friendReceive in FriendReceives)
            friendReceive.Dispose();
        FriendReceives.Clear();

        foreach (var friendRequest in FriendRequests)
            friendRequest.Dispose();
        FriendRequests.Clear();

        foreach (var groupFriend in GroupFriends)
            groupFriend.Dispose();
        GroupFriends.Clear();

        foreach (var friendChatDto in FriendChatDtos)
            friendChatDto.Dispose();
        FriendChatDtos.Clear();

        foreach (var groupChatDto in GroupChatDtos)
            groupChatDto.Dispose();
        GroupChatDtos.Clear();

        foreach (var groupGroupDto in GroupGroupDtos)
            groupGroupDto.Dispose();
        GroupGroupDtos.Clear();

        foreach (var groupReceived in GroupReceiveds)
            groupReceived.Dispose();
        GroupReceiveds.Clear();

        foreach (var groupRequest in GroupRequests)
            groupRequest.Dispose();
        GroupRequests.Clear();

        FriendReceives = null;
        FriendRequests = null;
        GroupFriends = null;
        FriendChatDtos = null;
        GroupChatDtos = null;
        GroupGroupDtos = null;
        GroupRequests = null;
        GroupReceiveds = null;

        UserDetail = null;
    }
}