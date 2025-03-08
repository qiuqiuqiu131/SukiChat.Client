using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.ChatUI;
using ChatClient.Tool.Data;

namespace ChatClient.Avalonia.Controls.GroupChatUI;

public partial class GroupChatUI : UserControl
{
    public GroupChatUI()
    {
        InitializeComponent();
    }

    private ScrollViewer ChatScroll;
    private ItemsControl ChatList;
    private Control Content;

    private bool isMovingToBottom = false;
    private CancellationTokenSource? AnimationToken;
    private double currentPosY = 0;

    private bool lockScroll = false;
    private double lockBottomDistance = 0;


    /// <summary>
    /// 当前可移动的最大偏移量
    /// </summary>
    private double MaxOffsetY
    {
        get
        {
            var offsetY = Content.Bounds.Height - ChatScroll.Bounds.Height;
            if (offsetY < 0) offsetY = 0;
            return offsetY;
        }
    }

    /// <summary>
    /// 记录当前Scroll的偏移量
    /// </summary>
    private double CurrentPosY
    {
        get => currentPosY;
        set
        {
            if (value < 0) currentPosY = 0;
            else
            {
                var maxValue = MaxOffsetY;
                if (value > maxValue)
                    currentPosY = maxValue;
                else
                    currentPosY = value;
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ChatScroll = e.NameScope.Get<ScrollViewer>("ChatScrollViewer");
        ChatScroll.PropertyChanged += ChatScrollOnPropertyChanged;
        ChatList = e.NameScope.Get<ItemsControl>("IC");
        Content = e.NameScope.Get<Control>("Content");
        Content.PropertyChanged += ContentOnPropertyChanged;

        // 捕获鼠标滚动事件
        var wheelEvent = Observable.FromEventPattern<PointerWheelEventArgs>(
            h => Content.PointerWheelChanged += h,
            h => Content.PointerWheelChanged -= h);

        wheelEvent.Subscribe(arg => arg.EventArgs.Handled = true);

        wheelEvent.Select(arg => -arg.EventArgs.Delta.Y * 100 + CurrentPosY)
            .Subscribe(ScrollMove);
    }

    #region Property

    #region Messages

    public static readonly StyledProperty<AvaloniaList<ChatData>> MessagesProperty =
        AvaloniaProperty.Register<GroupChatUI, AvaloniaList<ChatData>>(nameof(Messages),
            defaultValue: new AvaloniaList<ChatData>());

    public AvaloniaList<ChatData> Messages
    {
        get { return GetValue(MessagesProperty); }
        set { SetValue(MessagesProperty, value); }
    }

    #endregion

    #region HeadClickCommnad

    public static readonly StyledProperty<ICommand> HeadClickCommandProperty =
        AvaloniaProperty.Register<GroupChatUI, ICommand>(nameof(HeadClickCommand));

    public ICommand HeadClickCommand
    {
        get => GetValue(HeadClickCommandProperty);
        set => SetValue(HeadClickCommandProperty, value);
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
    private void ValueOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            // 最大可偏移量
            double maxOffsetY = MaxOffsetY;
            // 当前偏移量
            double OffsetYOrigion = ChatScroll.Offset.Y;
            if (isMovingToBottom // 正在移动到底部
                || Math.Abs(OffsetYOrigion - maxOffsetY) < 50 && e.NewStartingIndex != 0 // 靠近底部且不是扩展聊天记录
                || e.NewItems?[0] is ChatData chatData && e.NewStartingIndex != 0 && chatData.IsUser) // 是自己发送的消息
            {
                // 等待渲染完成
                await Task.Delay(50);
                ScrollToBottom();
            }
            else if (e.NewStartingIndex == 0)
            {
                lockScroll = true;
                lockBottomDistance = maxOffsetY - ChatScroll.Offset.Y;
                await Task.Delay(150);
                lockScroll = false;
                lockBottomDistance = 0;
            }
            else
            {
                //-- 新消息提醒 --//
                HaveUnReadMessage = true;
                int addCount = e.NewItems?.Count ?? 0;
                UnReadMessageCount += addCount;
            }
        }, DispatcherPriority.Render);
    }

    #endregion

    #region ControlPropertyChanged

    /// <summary>
    /// 监听Messages属性变化，更改事件绑定
    /// </summary>
    /// <param name="change"></param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MessagesProperty)
        {
            if (change.OldValue is AvaloniaList<ChatData> oldMessages)
                oldMessages.CollectionChanged -= ValueOnCollectionChanged;

            if (change.NewValue is AvaloniaList<ChatData> newMessages)
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
            double maxOffsetY = MaxOffsetY;
            // 当前偏移量
            double OffsetYOrigion = ChatScroll.Offset.Y;

            if (Math.Abs(OffsetYOrigion - MaxOffsetY) < 5)
                ChatScroll.Offset = new Vector(ChatScroll.Offset.X, maxOffsetY + 5);
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
            if (Math.Abs(ChatScroll.Offset.Y - MaxOffsetY) < 10)
            {
                HaveUnReadMessage = false;
                UnReadMessageCount = 0;
            }
        }
    }

    /// <summary>
    /// ChatUI中的内容发生变化后调用
    /// 1、BoundsProperty 当添加历史聊天记录时，会锁定Offset，使offset的值始终保持与最底端不变
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Control.BoundsProperty)
        {
            if (!lockScroll) return;
            double actualOffsetY = MaxOffsetY - lockBottomDistance;
            if (actualOffsetY < 0) actualOffsetY = 0;
            ChatScroll.Offset = new Vector(ChatScroll.Offset.X, actualOffsetY);
            CurrentPosY = actualOffsetY;
        }
    }

    #endregion

    /// <summary>
    /// 新消息气泡点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_OnClick(object? sender, RoutedEventArgs e) => ScrollToBottom(500);


    /// <summary>
    /// 当ChatUI绑定的ItemsSource发生变化时调用，当聊天对象发生变化时调用，将聊天框滚到最底部
    /// </summary>
    private async void OnItemsSourceChanged()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ChatScroll != null)
            {
                ChatScroll.ScrollToEnd();
                CurrentPosY = ChatScroll.Offset.Y;
            }
        }, DispatcherPriority.Render);
    }

    #region ScrollAnim

    private void ScrollMove(double target)
    {
        if (lockScroll) return;

        CurrentPosY = target;
        Dispatcher.UIThread.Invoke(() =>
        {
            if (isMovingToBottom) return;

            // 创建动画
            bool isMoving = AnimationToken is { Token.CanBeCanceled: true };
            double time = Math.Abs(target - ChatScroll.Offset.Y) * 2.5;
            if (time > 500) time = 500;
            var anim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(time),
                FillMode = FillMode.Forward,
                Easing = isMoving ? new SineEaseOut() : new SineEaseInOut(),
                IterationCount = new IterationCount(1),
                PlaybackDirection = PlaybackDirection.Normal,
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter { Property = ScrollViewer.OffsetProperty, Value = ChatScroll.Offset }
                        },
                        KeyTime = TimeSpan.FromSeconds(0)
                    },
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = ScrollViewer.OffsetProperty,
                                Value = new Vector(ChatScroll.Offset.X, target)
                            }
                        },
                        KeyTime = TimeSpan.FromMilliseconds(time)
                    }
                }
            };

            // 启动动画
            AnimationToken?.Cancel();
            AnimationToken = new CancellationTokenSource();
            anim.RunAsync(ChatScroll, AnimationToken.Token);
        });
    }

    private async void ScrollToBottom(int duration = 200)
    {
        HaveUnReadMessage = false;
        UnReadMessageCount = 0;

        // 计算移动量
        double scrollViewerHeight = ChatScroll.Bounds.Height;
        double contentHeight = Content.Bounds.Height;
        double targetOffsetY = contentHeight - scrollViewerHeight + 10;
        if (targetOffsetY < 0) targetOffsetY = 10;

        CurrentPosY = targetOffsetY - 10;

        // 启动动画
        AnimationToken?.Cancel();
        AnimationToken = new CancellationTokenSource();

        isMovingToBottom = true;

        // 创建动画
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            FillMode = FillMode.Forward,
            Easing = new SineEaseOut(),
            IterationCount = new IterationCount(1),
            PlaybackDirection = PlaybackDirection.Normal,
            Children =
            {
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter { Property = ScrollViewer.OffsetProperty, Value = ChatScroll.Offset }
                    },
                    KeyTime = TimeSpan.FromSeconds(0)
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = ScrollViewer.OffsetProperty,
                            Value = new Vector(ChatScroll.Offset.X, targetOffsetY)
                        }
                    },
                    KeyTime = TimeSpan.FromMilliseconds(duration)
                }
            }
        };

        // 等待动画完成
        await anim.RunAsync(ChatScroll, AnimationToken.Token);
        isMovingToBottom = false;
    }

    #endregion
}