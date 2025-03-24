using Avalonia.Controls;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.SearchUserGroupView.Region;

public partial class SearchFriendView : UserControl, IRegionMemberLifetime
{
    public SearchFriendView()
    {
        InitializeComponent();
    }

    public bool KeepAlive { get; } = false;
}