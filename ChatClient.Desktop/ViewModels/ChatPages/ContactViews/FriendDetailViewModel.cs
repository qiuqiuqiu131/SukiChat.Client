using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class FriendDetailViewModel : ViewModelBase
{
    private FriendRelationDto? _friend;

    public FriendRelationDto? Friend
    {
        get => _friend;
        set => SetProperty(ref _friend, value);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Friend = parameters.GetValue<FriendRelationDto>("dto");
    }
}