using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Android.Shared.Views.UserControls;

public partial class FriendChatUI : UserControl
{
    private Control ChatScrollContent;

    public FriendChatUI()
    {
        InitializeComponent();

        ChatScrollContent = (ChatScroll.Content as Control)!;
        ChatScroll.PropertyChanged += ChatScrollOnPropertyChanged;
        IC.Items.CollectionChanged += ValueOnCollectionChanged;

        // 捕获鼠标滚动事件
        var wheelEvent = Observable.FromEventPattern<PointerWheelEventArgs>(
            h => ChatScrollContent.PointerWheelChanged += h,
            h => ChatScrollContent.PointerWheelChanged -= h);

        wheelEvent.Subscribe(arg => arg.EventArgs.Handled = true);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ChatScroll.ScrollToEnd();
    }

    #region Property

    #region NotificationEvent

    public static readonly RoutedEvent<NotificationMessageEventArgs> NotificationEvent =
        RoutedEvent.Register<FriendChatUI, NotificationMessageEventArgs>(nameof(Notification),
            RoutingStrategies.Bubble);

    public event EventHandler<NotificationMessageEventArgs> Notification
    {
        add => AddHandler(NotificationEvent, value);
        remove => RemoveHandler(NotificationEvent, value);
    }

    #endregion

    #region UnReadMessage

    public static readonly StyledProperty<bool> HaveUnReadMessageProperty =
        AvaloniaProperty.Register<FriendChatUI, bool>(nameof(HaveUnReadMessage));

    public bool HaveUnReadMessage
    {
        get => GetValue(HaveUnReadMessageProperty);
        set => SetValue(HaveUnReadMessageProperty, value);
    }

    public static readonly StyledProperty<int> UnReadMessageCountProperty =
        AvaloniaProperty.Register<FriendChatUI, int>(nameof(UnReadMessageCount));

    public int UnReadMessageCount
    {
        get => GetValue(UnReadMessageCountProperty);
        set => SetValue(UnReadMessageCountProperty, value);
    }

    #endregion

    #region FileMessageClickCommand

    public static readonly StyledProperty<ICommand> FileMessageClickCommandProperty =
        AvaloniaProperty.Register<ChatMessageView, ICommand>(nameof(FileMessageClickCommand));

    public ICommand FileMessageClickCommand
    {
        get => GetValue(FileMessageClickCommandProperty);
        set => SetValue(FileMessageClickCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> FileRestoreCommandProperty =
        AvaloniaProperty.Register<ChatMessageView, ICommand>(nameof(FileRestoreCommand));

    public ICommand FileRestoreCommand
    {
        get => GetValue(FileRestoreCommandProperty);
        set => SetValue(FileRestoreCommandProperty, value);
    }

    #endregion

    #region ReCallCommand

    public static readonly StyledProperty<ICommand> ReCallCommandProperty =
        AvaloniaProperty.Register<FriendChatUI, ICommand>(
            "ReCallCommand");

    public ICommand ReCallCommand
    {
        get => GetValue(ReCallCommandProperty);
        set => SetValue(ReCallCommandProperty, value);
    }

    #endregion

    #endregion

    #region ValueChanged

    /// <summary>
    /// 当ChatUI绑定的Messages中的消息发生变化后调用
    /// 1、发送新消息
    /// 2、接受新消息
    /// 3、加载历史记录
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ValueOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ChatScroll == null) return;

        double maxOffsetY = ChatScroll.MaxOffsetY; // 最大可偏移量
        double OffsetYOrigion = ChatScroll.Offset.Y; // 当前偏移量

        Dispatcher.UIThread.Post(async () =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex == 0)
                {
                    // -- 加载历史聊天记录 --//
                    ChatScroll.IsHitTestVisible = true;
                }
                else if (Math.Abs(OffsetYOrigion - maxOffsetY) < 50 // 靠近底部且不是扩展聊天记录
                         || e.NewItems?[0] is ChatData chatData && chatData.IsUser) // 是自己发送的消息
                {
                    ChatScroll.ScrollToEnd();
                }
                else
                {
                    //-- 新消息提醒 --//
                    HaveUnReadMessage = true;
                    int addCount = e.NewItems?.Count ?? 0;
                    UnReadMessageCount += addCount;
                }
            }
        });
    }

    #endregion

    #region ControlPropertyChanged

    /// <summary>
    /// 获取ChatScroll的属性变化
    /// 1、ScrollBarMaximumProperty 当用户更改ChatUI的窗口大小时调用
    /// 2、OffsetProperty 当Scroll拖到最底部时，取消新消息气泡的显示
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ChatScrollOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ScrollBarMaximumProperty)
        {
            // 最大可偏移量
            double maxOffsetY = ChatScroll.MaxOffsetY;

            // 当ScrollViewer的视口宽度发生变化时，重新变更偏移量，保证底部固定不变
            if (Math.Abs(ChatScroll.Offset.Y - maxOffsetY) < 10)
                ChatScroll.Offset = new Vector(ChatScroll.Offset.X, maxOffsetY + 10);
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
            // 当ScrollViewer滚动到最底部时，取消新消息气泡的显示
            if (Math.Abs(ChatScroll.Offset.Y - ChatScroll.MaxOffsetY) < 10)
            {
                HaveUnReadMessage = false;
                UnReadMessageCount = 0;
            }
        }
    }

    #endregion


    /// <summary>
    /// 新消息气泡点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_OnClick(object? sender, RoutedEventArgs e) => ChatScroll.ScrollToEnd();

    /// <summary>
    /// 查看更多聊天记录按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 锁定ScrollViewer滚动
        ChatScroll.IsHitTestVisible = false;
        await Task.Delay(1500);
        ChatScroll.IsHitTestVisible = true;
    }
}