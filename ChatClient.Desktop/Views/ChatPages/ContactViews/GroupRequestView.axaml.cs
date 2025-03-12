using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class GroupRequestView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupRequestDto>> GroupRequestDtosProperty
        = AvaloniaProperty.Register<FriendRequestView, AvaloniaList<GroupRequestDto>>(
            nameof(GroupRequestDtos));

    public AvaloniaList<GroupRequestDto> GroupRequestDtos
    {
        get => GetValue(GroupRequestDtosProperty);
        set => SetValue(GroupRequestDtosProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<GroupReceivedDto>> GroupReceivedDtosProperty =
        AvaloniaProperty.Register<FriendRequestView, AvaloniaList<GroupReceivedDto>>(
            nameof(GroupReceivedDtos));

    public AvaloniaList<GroupReceivedDto> GroupReceivedDtos
    {
        get => GetValue(GroupReceivedDtosProperty);
        set => SetValue(GroupReceivedDtosProperty, value);
    }

    private bool isInit;
    private ItemCollection _itemCollection;

    public GroupRequestView()
    {
        InitializeComponent();
        _itemCollection = RequestItemsControl.Items;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (isInit) return;
        isInit = true;

        GroupReceivedDtos.CollectionChanged += OnCollectionChanged;
        GroupRequestDtos.CollectionChanged += OnCollectionChanged;
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
        while (requestIndex < GroupRequestDtos.Count && receivedIndex < GroupReceivedDtos.Count)
        {
            var requestDto = GroupRequestDtos[requestIndex];
            var receivedDto = GroupReceivedDtos[receivedIndex];

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
        while (requestIndex < GroupRequestDtos.Count)
        {
            mergedList.Add(GroupRequestDtos[requestIndex]);
            requestIndex++;
        }

        // 添加剩余的 FriendReceivedDtos
        while (receivedIndex < GroupReceivedDtos.Count)
        {
            mergedList.Add(GroupReceivedDtos[receivedIndex]);
            receivedIndex++;
        }

        // 清空 _itemCollection 并添加排序后的项目
        foreach (var item in mergedList)
            _itemCollection.Add(item);
    }
}