using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class AddNewFriendView : UserControl
{
    public AddNewFriendView()
    {
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SearchBox != null)
            SearchBox.Focus();
    }
}