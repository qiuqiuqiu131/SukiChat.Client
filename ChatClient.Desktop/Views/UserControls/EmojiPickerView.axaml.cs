using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.UserControls;

public partial class EmojiPickerView : UserControl
{
    public static readonly RoutedEvent<EmojiSelectedEventArgs> EmojiSelectedEvent =
        RoutedEvent.Register<EmojiPickerView, EmojiSelectedEventArgs>(
            nameof(EmojiSelected), RoutingStrategies.Bubble);

    public event EventHandler<EmojiSelectedEventArgs> EmojiSelected
    {
        add => AddHandler(EmojiSelectedEvent, value);
        remove => RemoveHandler(EmojiSelectedEvent, value);
    }

    public EmojiPickerView()
    {
        InitializeComponent();

        // 这里添加一些常用emoji表情
        var emojis = new[]
        {
            "😀", "😃", "😄", "😁", "😅", "😂", "🤣", "😊", "😇", "😉", "😍", "🥰",
            "😘", "😋", "😛", "😜", "😝", "🤑", "🤗", "🤔", "🤭", "🤫", "🤥", "😶",
            "😐", "😑", "😬", "🙄", "😯", "😦", "😧", "😮", "😲", "😴", "🤤", "😪",
            "😵", "🤐", "🥴", "🤢", "🤮", "🤧", "😷", "🤒", "🤕", "🤑", "🤠", "😈"
        };

        EmojiList.ItemsSource = emojis;
    }

    public void SelectEmoji(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && button.CommandParameter is string emoji)
        {
            RaiseEvent(new EmojiSelectedEventArgs(EmojiSelectedEvent, emoji));
        }
    }
}

public class EmojiSelectedEventArgs : RoutedEventArgs
{
    public string Emoji { get; }

    public EmojiSelectedEventArgs(RoutedEvent routedEvent, string emoji)
        : base(routedEvent)
    {
        Emoji = emoji;
    }
}