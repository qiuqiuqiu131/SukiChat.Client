using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Data;

public class OutlineMessageDto
{
    public List<FriendRequestMessage> FriendRequestMessages { get; set; }
    public List<NewFriendMessage> NewFriendMessages { get; set; }
    public List<FriendChatMessage> FriendChatMessages { get; set; }
    public List<GroupChatMessage> GroupChatMessages { get; set; }
}