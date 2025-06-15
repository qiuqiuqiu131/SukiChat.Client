using ChatClient.Tool.Data.Friend;

namespace ChatClient.Tool.Data.SearchData;

public class FriendSearchDto : BindableBase
{
    private FriendRelationDto _friendRelationDto;

    public FriendRelationDto FriendRelationDto
    {
        get => _friendRelationDto;
        set => SetProperty(ref _friendRelationDto, value);
    }

    private string message;

    public string Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }
}