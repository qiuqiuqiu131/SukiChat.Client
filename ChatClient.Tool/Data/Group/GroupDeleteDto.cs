namespace ChatClient.Tool.Data.Group;

public class GroupDeleteDto
{
    public string DeleteId { get; set; }
    public string GroupId { get; set; }

    public string MemberId { get; set; }

    public int DeleteMethod { get; set; }

    public string OperateUserId { get; set; }

    public DateTime DeleteTime { get; set; }
}