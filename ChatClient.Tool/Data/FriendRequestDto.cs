namespace ChatClient.Tool.Data;

public class FriendRequestDto : BindableBase
{
    public string UserFromId {  get; set; }
    
    public string UserTargetId { get; set; }
    
    public string Group { get; set; }
    
    public DateTime RequestTime { get; set; }
}