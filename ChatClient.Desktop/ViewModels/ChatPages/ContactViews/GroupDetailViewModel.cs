using ChatClient.Tool.Common;
using ChatClient.Tool.Data.Group;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class GroupDetailViewModel : ViewModelBase
{
    private GroupRelationDto? _group;

    public GroupRelationDto? Friend
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Friend = parameters.GetValue<GroupRelationDto>("dto");
    }
}