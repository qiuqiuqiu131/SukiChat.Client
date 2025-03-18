using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

    public static readonly StyledProperty<AvaloniaList<GroupDeleteDto>> GroupDeleteDtosProperty =
        AvaloniaProperty.Register<FriendRequestView, AvaloniaList<GroupDeleteDto>>(
            nameof(GroupDeleteDtos));

    public AvaloniaList<GroupDeleteDto> GroupDeleteDtos
    {
        get => GetValue(GroupDeleteDtosProperty);
        set => SetValue(GroupDeleteDtosProperty, value);
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
        GroupDeleteDtos.CollectionChanged += OnCollectionChanged;
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

        // 将所有项目添加到合并列表中
        foreach (var requestDto in GroupRequestDtos)
            mergedList.Add(requestDto);

        foreach (var receivedDto in GroupReceivedDtos)
            mergedList.Add(receivedDto);

        foreach (var deleteDto in GroupDeleteDtos)
            mergedList.Add(deleteDto);

        // 按时间戳降序排序
        mergedList.Sort((a, b) =>
        {
            var timeA = GetTimestamp(a);
            var timeB = GetTimestamp(b);
            // 降序排序，所以比较 B 和 A
            return timeB.CompareTo(timeA);
        });

        // 清空 _itemCollection
        _itemCollection.Clear();

        // 添加排序后的项目
        foreach (var item in mergedList)
            _itemCollection.Add(item);
    }

    // 辅助方法：从不同类型的对象获取时间戳
    private DateTime GetTimestamp(object item)
    {
        return item switch
        {
            GroupRequestDto requestDto => requestDto.RequestTime,
            GroupReceivedDto receiveDto => receiveDto.ReceiveTime,
            GroupDeleteDto deleteDto => deleteDto.DeleteTime, // 假设有DeleteTime属性
            _ => DateTime.MinValue // 默认值，确保未知类型排在最后
        };
    }
}