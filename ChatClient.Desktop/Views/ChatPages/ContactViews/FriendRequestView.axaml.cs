using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    public static readonly StyledProperty<AvaloniaList<FriendDeleteDto>> FriendDeleteDtosProperty
        = AvaloniaProperty.Register<FriendRequestView, AvaloniaList<FriendDeleteDto>>(
            nameof(FriendDeleteDtos));

    public AvaloniaList<FriendDeleteDto> FriendDeleteDtos
    {
        get => GetValue(FriendDeleteDtosProperty);
        set => SetValue(FriendDeleteDtosProperty, value);
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
        FriendDeleteDtos.CollectionChanged += OnCollectionChanged;
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
        foreach (var requestDto in FriendRequestDtos)
            mergedList.Add(requestDto);

        foreach (var receivedDto in FriendReceivedDtos)
            mergedList.Add(receivedDto);

        foreach (var deleteDto in FriendDeleteDtos)
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
            FriendRequestDto requestDto => requestDto.RequestTime,
            FriendReceiveDto receiveDto => receiveDto.ReceiveTime,
            FriendDeleteDto deleteDto => deleteDto.DeleteTime, // 假设有DeleteTime属性
            _ => DateTime.MinValue // 默认值，确保未知类型排在最后
        };
    }
}