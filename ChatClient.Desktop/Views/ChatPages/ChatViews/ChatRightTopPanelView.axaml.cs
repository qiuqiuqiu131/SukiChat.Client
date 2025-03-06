using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.ChatUI;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatRightTopPanelView : UserControl
{
    public ChatRightTopPanelView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var ViewModel = DataContext as ChatRightTopPanelViewModel;
        //ViewModel!.ChatViewModel.OnFriendSelectionChanged += ChatUI.OnItemsSourceChanged;
    }
}