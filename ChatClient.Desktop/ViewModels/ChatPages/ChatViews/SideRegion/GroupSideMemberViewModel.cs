using ChatClient.Tool.Data.Group;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.SideRegion;

public class GroupSideMemberViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private IRegionNavigationService _regionManager;

    private GroupRelationDto _selectedGroup;

    public GroupRelationDto SelectedGroup
    {
        get => _selectedGroup;
        set => SetProperty(ref _selectedGroup, value);
    }

    public DelegateCommand ReturnCommand { get; }

    public GroupSideMemberViewModel()
    {
        ReturnCommand = new DelegateCommand(Return);
    }

    private void Return()
    {
        _regionManager.Journal.GoBack();
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedGroup = navigationContext.Parameters.GetValue<GroupRelationDto>("SelectedGroup");
        _regionManager = navigationContext.NavigationService;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    #endregion

    public bool KeepAlive => false;
}