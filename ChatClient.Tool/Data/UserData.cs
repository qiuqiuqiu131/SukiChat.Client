using System.Collections.ObjectModel;
using Avalonia.Collections;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Data;

public class UserData
{
    public UserDto UserDetail { get; set; }

    public AvaloniaList<FriendReceiveDto> FriendReceives { get; set; }

    public AvaloniaList<GroupFriendDto> GroupFriends { get; set; }

    public AvaloniaList<FriendChatDto> FriendChatDtos { get; set; }

    public AvaloniaList<GroupChatDto> GroupChatDtos { get; set; }

    public void Clear()
    {
        FriendReceives.Clear();
        GroupFriends.Clear();
        FriendChatDtos.Clear();
        GroupChatDtos.Clear();

        FriendReceives = null;
        GroupFriends = null;
        FriendChatDtos = null;
        GroupChatDtos = null;

        UserDetail = null;
    }
}