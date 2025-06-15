namespace ChatClient.Tool.Events;

public class RegionNavigationEvent:PubSubEvent<RegionNavigationEventArgs>
{
    
}

public class RegionNavigationEventArgs
{
    public string RegionName { get; set; }
    public string ViewName { get; set; }
    public INavigationParameters Parameters { get; set; }
}