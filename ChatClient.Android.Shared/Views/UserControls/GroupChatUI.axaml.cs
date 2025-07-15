using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Android.Shared.Views.UserControls;

public partial class GroupChatUI : UserControl
{
    public GroupChatUI()
    {
        InitializeComponent();
    }

    #region ScrollField

    private Avalonia.Controls.QScrollViewer.QScrollViewer ChatScroll;
    private Control ChatScrollContent;

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ChatScroll = e.NameScope.Get<Avalonia.Controls.QScrollViewer.QScrollViewer>("ChatScrollViewer");
        ChatScrollContent = ChatScroll.Content as Control ??
                            throw new InvalidOperationException("ChatScroll.Content must be a Control");
        ChatScroll.PropertyChanged += ChatScrollOnPropertyChanged;
        ChatScroll.GotFocus += (sender, args) => { ChatScroll.Offset = new Vector(0, ChatScroll.CurrentPos); };

        // 捕获鼠标滚动事件
        var wheelEvent = Observable.FromEventPattern<PointerWheelEventArgs>(
            h => ChatScrollContent.PointerWheelChanged += h,
            h => ChatScrollContent.PointerWheelChanged -= h);

        wheelEvent.Subscribe(arg => arg.EventArgs.Handled = true);

        wheelEvent.Select(arg => -arg.EventArgs.Delta.Y * 150)
            .Subscribe(d => ChatScroll.MoveUp(d));
    }

    #region Property

    #region Messages

    public static readonly StyledProperty<AvaloniaList<GroupChatData>> MessagesProperty =
        AvaloniaProperty.Register<GroupChatUI, AvaloniaList<GroupChatData>>(nameof(Messages),
            defaultValue: new AvaloniaList<GroupChatData>());

    public AvaloniaList<GroupChatData> Messages
    {
        get { return GetValue(MessagesProperty); }
        set { SetValue(MessagesProperty, value); }
    }

    #endregion

    #region SearchMoreCommand

    public static readonly StyledProperty<ICommand> SearchMoreCommandProperty =
        AvaloniaProperty.Register<GroupChatUI, ICommand>(nameof(SearchMoreCommand));

    public ICommand SearchMoreCommand
    {
        get => GetValue(SearchMoreCommandProperty);
        set => SetValue(SearchMoreCommandProperty, value);
    }

    #endregion

    #region NotificationEvent

    public static readonly RoutedEvent<NotificationMessageEventArgs> NotificationEvent =
        RoutedEvent.Register<GroupChatUI, NotificationMessageEventArgs>(nameof(Notification), RoutingStrategies.Bubble);

    public event EventHandler<NotificationMessageEventArgs> Notification
    {
        add => AddHandler(NotificationEvent, value);
        remove => RemoveHandler(NotificationEvent, value);
    }

    #endregion

    #region SearchMoreVisible

    public static readonly StyledProperty<bool> SearchMoreVisibleProperty =
        AvaloniaProperty.Register<GroupChatUI, bool>(nameof(SearchMoreVisible), defaultValue: false);

    public bool SearchMoreVisible
    {
        get => GetValue(SearchMoreVisibleProperty);
        set => SetValue(SearchMoreVisibleProperty, value);
    }

    #endregion

    #region UserImage

    public static readonly StyledProperty<IImage?> UserImageSourceProperty =
        AvaloniaProperty.Register<GroupChatUI, IImage?>(nameof(UserImageSource));

    public IImage? UserImageSource
    {
        get => GetValue(UserImageSourceProperty);
        set => SetValue(UserImageSourceProperty, value);
    }

    #endregion

    #region UnReadMessage

    public static readonly StyledProperty<bool> HaveUnReadMessageProperty =
        AvaloniaProperty.Register<GroupChatUI, bool>(nameof(HaveUnReadMessage));

    public bool HaveUnReadMessage
    {
        get => GetValue(HaveUnReadMessageProperty);
        set => SetValue(HaveUnReadMessageProperty, value);
    }

    public static readonly StyledProperty<int> UnReadMessageCountProperty =
        AvaloniaProperty.Register<GroupChatUI, int>(nameof(UnReadMessageCount));

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

    #region DeleteMessageCommand

    public static readonly StyledProperty<ICommand> DeleteMessageCommandProperty =
        AvaloniaProperty.Register<GroupChatUI, ICommand>(
            "DeleteMessageCommand");

    public ICommand DeleteMessageCommand
    {
        get => GetValue(DeleteMessageCommandProperty);
        set => SetValue(DeleteMessageCommandProperty, value);
    }

    #endregion

    #region RetractMessageCommand

    public static readonly StyledProperty<ICommand> RetractMessageCommandProperty =
        AvaloniaProperty.Register<GroupChatUI, ICommand>(
            "RetractMessageCommand");

    public ICommand RetractMessageCommand
    {
        get => GetValue(RetractMessageCommandProperty);
        set => SetValue(RetractMessageCommandProperty, value);
    }

    #endregion

    #region ShareMessageCommand

    public static readonly StyledProperty<ICommand> ShareMessageCommandProperty =
        AvaloniaProperty.Register<GroupChatUI, ICommand>(
            "ShareMessageCommand");

    public ICommand ShareMessageCommand
    {
        get => GetValue(ShareMessageCommandProperty);
        set => SetValue(ShareMessageCommandProperty, value);
    }

    #endregion

    #region ContextMenuShow

    public static readonly RoutedEvent<RoutedEventArgs> ContextMenuShowEvent =
        RoutedEvent.Register<GroupChatUI, RoutedEventArgs>(nameof(ContextMenuShow), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> ContextMenuShow
    {
        add => AddHandler(NotificationEvent, value);
        remove => RemoveHandler(NotificationEvent, value);
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
        double maxOffsetY = ChatScroll.MaxOffsetY; // 最大可偏移量
        double OffsetYOrigion = ChatScroll.Offset.Y; // 当前偏移量
        Dispatcher.UIThread.Post(async () =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex == 0)
                {
                    // -- 加载历史聊天记录 --//
                    ChatScroll.UnLock();
                }
                else if (ChatScroll.IsToMoving
                         || Math.Abs(OffsetYOrigion - maxOffsetY) < 50 // 靠近底部且不是扩展聊天记录
                         || e.NewItems?[0] is GroupChatData chatData && chatData.IsUser) // 是自己发送的消息
                {
                    ChatScroll.ScrollToBottom();
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
    /// 监听Messages属性变化，更改事件绑定
    /// </summary>
    /// <param name="change"></param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MessagesProperty)
        {
            if (change.OldValue is AvaloniaList<GroupChatData> oldMessages)
                oldMessages.CollectionChanged -= ValueOnCollectionChanged;

            if (change.NewValue is AvaloniaList<GroupChatData> newMessages)
                newMessages.CollectionChanged += ValueOnCollectionChanged;

            OnItemsSourceChanged();
        }
    }

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
            // 当前偏移量
            double OffsetYOrigion = ChatScroll.Offset.Y;

            if (Math.Abs(OffsetYOrigion - maxOffsetY) < 5)
                ChatScroll.Offset = new Vector(ChatScroll.Offset.X, maxOffsetY + 5);
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
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
    private void Button_OnClick(object? sender, RoutedEventArgs e) => ChatScroll.MoveToBottom();

    /// <summary>
    /// 查看更多聊天记录按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 锁定ScrollViewer滚动
        ChatScroll.Lock();
        await Task.Delay(1500);
        ChatScroll.UnLock();
    }


    /// <summary>
    /// 当ChatUI绑定的ItemsSource发生变化时调用，当聊天对象发生变化时调用，将聊天框滚到最底部
    /// </summary>
    private void OnItemsSourceChanged()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (ChatScroll != null)
                ChatScroll.Opacity = 0;
        });
        Dispatcher.UIThread.Post(() =>
        {
            ChatScroll.Opacity = 1;
            ChatScroll.ScrollToBottom();
        }, DispatcherPriority.Background);
    }
}