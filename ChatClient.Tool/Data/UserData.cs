using System.Collections.ObjectModel;
using Avalonia.Collections;

namespace ChatClient.Tool.Data;

public class UserData
{
    public UserDto UserDetail { get; set; }

    public List<UserDto> Friends { get; set; } = new();

    public AvaloniaList<FriendReceiveDto> FriendReceives { get; set; }

    public AvaloniaList<GroupFriendDto> GroupFriends { get; set; }

    public AvaloniaList<FriendChatDto> FriendChatDtos { get; set; }

    public void Clear()
    {
        FriendReceives.Clear();
        GroupFriends.Clear();
        FriendChatDtos.Clear();

        FriendReceives = null;
        GroupFriends = null;
        FriendChatDtos = null;

        UserDetail = null;
    }
}