using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChatClient.Tool.Data;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class FriendRequestView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<FriendRequestDto>> FriendRequestDtosProperty
        = AvaloniaProperty.Register<FriendRequestView, AvaloniaList<FriendRequestDto>>(
            nameof(FriendRequestDtos));

    public AvaloniaList<FriendRequestDto> FriendRequestDtos
    {
        get => GetValue(FriendRequestDtosProperty);
        set => SetValue(FriendRequestDtosProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<FriendReceiveDto>> FriendReceivedDtosProperty =
        AvaloniaProperty.Register<FriendRequestView, AvaloniaList<FriendReceiveDto>>(
            nameof(FriendReceiveDto));

    public AvaloniaList<FriendReceiveDto> FriendReceivedDtos
    {
        get => GetValue(FriendReceivedDtosProperty);
        set => SetValue(FriendReceivedDtosProperty, value);
    }

    private bool isInit;
    private ItemCollection _itemCollection;

    public FriendRequestView()
    {
        InitializeComponent();
        _itemCollection = RequestItemsControl.Items;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (isInit) return;
        isInit = true;

        FriendReceivedDtos.CollectionChanged += OnCollectionChanged;
        FriendRequestDtos.CollectionChanged += OnCollectionChanged;
        InitItems();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var newItem in e.NewItems)
                    _itemCollection.Insert(0, newItem);
            }
        });
    }

    private void InitItems()
    {
        // 创建一个新的列表来存储合并后的结果
        var mergedList = new List<object>();

        // 使用两个索引来遍历两个列表
        int requestIndex = 0;
        int receivedIndex = 0;

        // 合并两个列表
        while (requestIndex < FriendRequestDtos.Count && receivedIndex < FriendReceivedDtos.Count)
        {
            var requestDto = FriendRequestDtos[requestIndex];
            var receivedDto = FriendReceivedDtos[receivedIndex];

            // 比较两个项目的时间戳，确保降序排序
            if (requestDto.RequestTime >= receivedDto.ReceiveTime)
            {
                mergedList.Add(requestDto);
                requestIndex++;
            }
            else
            {
                mergedList.Add(receivedDto);
                receivedIndex++;
            }
        }

        // 添加剩余的 FriendRequestDtos
        while (requestIndex < FriendRequestDtos.Count)
        {
            mergedList.Add(FriendRequestDtos[requestIndex]);
            requestIndex++;
        }

        // 添加剩余的 FriendReceivedDtos
        while (receivedIndex < FriendReceivedDtos.Count)
        {
            mergedList.Add(FriendReceivedDtos[receivedIndex]);
            receivedIndex++;
        }

        // 清空 _itemCollection 并添加排序后的项目
        foreach (var item in mergedList)
            _itemCollection.Add(item);
    }
}