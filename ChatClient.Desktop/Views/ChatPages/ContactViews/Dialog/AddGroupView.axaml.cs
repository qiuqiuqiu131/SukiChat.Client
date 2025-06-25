using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;

public partial class AddGroupView : UserControl
{
    public AddGroupView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        InputBox.Focus();
    }
}