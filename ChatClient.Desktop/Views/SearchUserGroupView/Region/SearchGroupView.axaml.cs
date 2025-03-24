using Avalonia.Controls;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.SearchUserGroupView.Region;

public partial class SearchGroupView : UserControl, IRegionMemberLifetime
{
    public SearchGroupView()
    {
        InitializeComponent();
    }

    public bool KeepAlive { get; } = false;
}