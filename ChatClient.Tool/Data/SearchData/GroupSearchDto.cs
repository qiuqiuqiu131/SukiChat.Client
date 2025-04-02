using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Data.SearchData;

public class GroupSearchDto : BindableBase
{
    private GroupRelationDto _groupRelationDto;

    public GroupRelationDto GroupRelationDto
    {
        get => _groupRelationDto;
        set => SetProperty(ref _groupRelationDto, value);
    }

    private string message;

    public string Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }
}