using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Tool.Data;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatLeftPanelView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<FriendChatDto>> ItemsSourceProperty =
        AvaloniaProperty.Register<ChatLeftPanelView, AvaloniaList<FriendChatDto>>(nameof(ItemsSource));

    public AvaloniaList<FriendChatDto> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private IDataTemplate? _dataTemplate;
    private ItemCollection _itemCollection;

    private bool isLoaded = false;

    public ChatLeftPanelView()
    {
        InitializeComponent();

        _dataTemplate = Items.ItemTemplate;
        _itemCollection = Items.Items;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (isLoaded) return;
        ItemsSource.CollectionChanged += ItemsSourceOnCollectionChanged;
        InitItemsControl();
        isLoaded = true;
    }

    private void InitItemsControl()
    {
        foreach (var items in ItemsSource)
        {
            items.OnLastChatMessagesChanged += SortItemControl;

            var contorl = _dataTemplate?.Build(items);
            if (contorl != null)
            {
                contorl.DataContext = items;
                _itemCollection.Add(contorl);
            }
        }
    }


    private void ItemsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    for (int i = e.NewItems.Count - 1; i >= 0; i--)
                        _itemCollection.Insert(0, e.NewItems[i]);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        _itemCollection.Remove(item);
                        ((Control)item).DataContext = null;
                    }
                }
            }
        });
    }

    // 排序
    private void SortItemControl(FriendChatDto friend)
    {
        // 找到对应的控件
        var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == friend);

        _itemCollection.Remove(control);
        _itemCollection.Insert(0, control);
    }

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
    }
}