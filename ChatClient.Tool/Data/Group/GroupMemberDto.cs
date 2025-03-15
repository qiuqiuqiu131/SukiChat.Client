using Avalonia.Media.Imaging;

namespace ChatClient.Tool.Data.Group;

public class GroupMemberDto : BindableBase, IDisposable
{
    public string UserId
    {
        get => userId;
        set => SetProperty(ref userId, value);
    }

    private string userId;

    public string GroupId
    {
        get => groupId;
        set => SetProperty(ref groupId, value);
    }

    private string groupId;

    public DateTime JoinTime
    {
        get => joinTime;
        set => SetProperty(ref joinTime, value);
    }

    private DateTime joinTime;

    public int Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    private int status;

    public string NickName
    {
        get => nickName;
        set => SetProperty(ref nickName, value);
    }

    private string nickName;

    public int HeadIndex
    {
        get => headIndex;
        set => SetProperty(ref headIndex, value);
    }

    private int headIndex;

    public Bitmap? HeadImage
    {
        get => headImage;
        set => SetProperty(ref headImage, value);
    }

    private Bitmap? headImage;

    public void CopyFrom(GroupMemberDto groupMemberDto)
    {
        HeadIndex = groupMemberDto.headIndex;
        HeadImage = groupMemberDto.headImage;
        NickName = groupMemberDto.nickName;
        Status = groupMemberDto.status;
    }

    public void Dispose()
    {
        headImage?.Dispose();
    }
}