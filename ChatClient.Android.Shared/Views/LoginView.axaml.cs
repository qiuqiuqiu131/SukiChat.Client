using Avalonia.Controls;
using Avalonia.Data;
using ChatClient.Tool.Data;

namespace ChatClient.Android.Shared.Views;

[RegionMemberLifetime(KeepAlive = false)]
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        IDBox.ValueMemberBinding = new Binding("ID");
        IDBox.ItemFilter = (searchText, item) =>
        {
            if (item is LoginUserItem loginUserItem && !string.IsNullOrEmpty(searchText))
                return loginUserItem.ID.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
            return false;
        };
    }
}