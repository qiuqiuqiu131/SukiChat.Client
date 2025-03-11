namespace ChatClient.Tool.Events;

/// <summary>
/// 传入一个FriendRelationDto或GroupRelationDto对象，用于向视图发送消息
/// </summary>
public class SendMessageToViewEvent : PubSubEvent<object>
{
}