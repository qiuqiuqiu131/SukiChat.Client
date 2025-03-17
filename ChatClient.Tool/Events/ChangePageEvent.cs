namespace ChatClient.Tool.Events;

public class ChangePageEvent : PubSubEvent<ChatPageChangedContext>
{
}

public class ChatPageChangedContext
{
    public string PageName { get; set; }
    public INavigationParameters Parameters { get; set; }
}