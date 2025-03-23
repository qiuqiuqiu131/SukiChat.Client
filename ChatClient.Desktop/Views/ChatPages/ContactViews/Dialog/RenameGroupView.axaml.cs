using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;

public partial class RenameGroupView : UserControl
{
    public RenameGroupView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        InputBox.Focus();
        InputBox.SelectionStart = InputBox.Text.Length;
    }
}