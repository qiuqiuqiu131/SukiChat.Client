namespace ChatClient.Tool.Events;

public class ChatPageUnreadCountChangedEvent : PubSubEvent<(string, int)>
{
}