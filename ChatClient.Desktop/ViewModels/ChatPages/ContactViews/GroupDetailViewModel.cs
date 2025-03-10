using ChatClient.Tool.Common;
using ChatClient.Tool.Data.Group;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class GroupDetailViewModel : ViewModelBase
{
    private GroupRelationDto? _group;

    public GroupRelationDto? Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Group = parameters.GetValue<GroupRelationDto>("dto");
    }
}