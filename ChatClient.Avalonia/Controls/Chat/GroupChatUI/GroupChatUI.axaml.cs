using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat.ChatUI;
using ChatClient.Tool.Data;
using ChatClient.Tool.Tools;
using Material.Icons;
using Material.Icons.Avalonia;

namespace ChatClient.Avalonia.Controls.Chat.GroupChatUI;

public partial class GroupChatUI : UserControl
{
    private ContextMenu? _contextMenu;

    public GroupChatUI()
    {
        InitializeComponent();
    }

    #region ScrollField

    private ScrollViewer ChatScroll;
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

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ChatScroll = e.NameScope.Get<ScrollViewer>("ChatScrollViewer");
        ChatScroll.PropertyChanged += ChatScrollOnPropertyChanged;
        ChatScroll.GotFocus += (sender, args) => { ChatScroll.Offset = new Vector(0, CurrentPosY); };
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

    public static readonly StyledProperty<AvaloniaList<GroupChatData>> MessagesProperty =
        AvaloniaProperty.Register<GroupChatUI, AvaloniaList<GroupChatData>>(nameof(Messages),
            defaultValue: new AvaloniaList<GroupChatData>());

    public AvaloniaList<GroupChatData> Messages
    {
        get { return GetValue(MessagesProperty); }
        set { SetValue(MessagesProperty, value); }
    }

    #endregion

    #region HeadClickEvent

    public event EventHandler<GroupHeadClickEventArgs> HeadClick;

    public static readonly RoutedEvent<GroupHeadClickEventArgs> HeadClickEvent =
        RoutedEvent.Register<GroupChatUI, GroupHeadClickEventArgs>(nameof(HeadClick), RoutingStrategies.Bubble);

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
                || e.NewItems?[0] is GroupChatData chatData && e.NewStartingIndex != 0 && chatData.IsUser) // 是自己发送的消息
            {
                // 等待渲染完成
                await Task.Delay(50);
                ScrollToBottom();
            }
            else if (e.NewStartingIndex == 0)
            {
                lockScroll = true;
                lockBottomDistance = maxOffsetY - ChatScroll.Offset.Y;
                await Task.Delay(200);
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
            ChatScroll.UpdateLayout();
            ChatScroll.ScrollToEnd();
            CurrentPosY = ChatScroll.Offset.Y;
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

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            if (sender is Control control)
            {
                if (control.DataContext is GroupChatData chatData)
                {
                    RaiseEvent(new GroupHeadClickEventArgs(sender, HeadClickEvent, chatData, e));
                }
            }
        }
    }

    // 消息被点击
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ChatData chatData &&
            e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
        {
            // 如果点击了右键
            if (_contextMenu != null)
            {
                _contextMenu.Close();
                _contextMenu = null;
            }

            _contextMenu = CreateMenu(chatData);
            _contextMenu.Placement = PlacementMode.Pointer;
            // 获取鼠标位置
            var position = e.GetPosition(this);
            if (position.X > Bounds.Width / 2)
                _contextMenu.PlacementAnchor = PopupAnchor.TopRight;
            else
                _contextMenu.PlacementAnchor = PopupAnchor.TopLeft;

            _contextMenu.Open(this);
        }
    }

    private ContextMenu? CreateMenu(ChatData chatData)
    {
        if (chatData.ChatMessages.Count != 1) return null;
        ContextMenu contextMenu = new ContextMenu();
        if (chatData.ChatMessages[0].Content is TextMessDto textMessDto)
        {
            var item1 = new MenuItem { Header = "复制", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy } };
            item1.Click += (s, e) =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                topLevel?.Clipboard?.SetTextAsync(textMessDto.Text);
                RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "已复制到剪贴板",
                    NotificationType.Information));
            };
            contextMenu.Items.Add(item1);
        }
        else if (chatData.ChatMessages[0].Content is ImageMessDto imageMessDto && imageMessDto.ImageSource is not null)
        {
            var item1 = new MenuItem { Header = "复制", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy } };
            item1.Click += (s, e) =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Files, imageMessDto.ImageSource);
                topLevel?.Clipboard?.SetDataObjectAsync(dataObject);
                RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "图片已复制到剪贴板",
                    NotificationType.Information));
            };
            contextMenu.Items.Add(item1);

            var item2 = new MenuItem
                { Header = "打开图片", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOpenOutline } };
            item2.Click += (s, e) => { ImageTool.OpenImageInSystemViewer(imageMessDto.ImageSource); };
            contextMenu.Items.Add(item2);

            var item3 = new MenuItem
                { Header = "打开所在文件夹", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOutline } };
            item3.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(imageMessDto.ActualPath) && System.IO.File.Exists(imageMessDto.ActualPath))
                {
                    var argument = $"/select,\"{imageMessDto.ActualPath}\"";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
                    {
                        UseShellExecute = true
                    });
                }
            };
            contextMenu.Items.Add(item3);
        }
        else if (chatData.ChatMessages[0].Content is FileMessDto fileMessDto)
        {
            if (fileMessDto.IsSuccess)
            {
                var item1 = new MenuItem
                    { Header = "打开文件", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOpenOutline } };
                item1.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(fileMessDto.TargetFilePath) &&
                        System.IO.File.Exists(fileMessDto.TargetFilePath))
                    {
                        var argument = $"/select,\"{fileMessDto.TargetFilePath}\"";
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
                            {
                                UseShellExecute = true
                            });
                    }
                    else
                        fileMessDto.IsSuccess = false;
                };
                contextMenu.Items.Add(item1);

                var item2 = new MenuItem
                    { Header = "打开所在文件夹", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOutline } };
                item2.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(fileMessDto.TargetFilePath) &&
                        System.IO.File.Exists(fileMessDto.TargetFilePath))
                    {
                        var argument = $"/select,\"{fileMessDto.TargetFilePath}\"";
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
                            {
                                UseShellExecute = true
                            });
                    }
                    else
                        fileMessDto.IsSuccess = false;
                };
                contextMenu.Items.Add(item2);
            }
            else if (fileMessDto.IsExit)
            {
                var item1 = new MenuItem
                    { Header = "下载", Icon = new MaterialIcon { Kind = MaterialIconKind.DownloadOutline } };
                item1.Click += (s, e) => FileMessageClickCommand.Execute(fileMessDto);
                contextMenu.Items.Add(item1);
            }

            if (fileMessDto.IsSuccess || fileMessDto.IsExit)
            {
                var item3 = new MenuItem
                    { Header = "另存为", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentSaveMoveOutline } };
                item3.Click += (s, e) => FileRestoreCommand.Execute(fileMessDto);
                contextMenu.Items.Add(item3);
            }
        }

        var comItem1 = new MenuItem
            { Header = "转发", Icon = new MaterialIcon { Kind = MaterialIconKind.Forwardburger } };
        contextMenu.Items.Add(comItem1);

        var comItem2 = new MenuItem
            { Header = "引用", Icon = new MaterialIcon { Kind = MaterialIconKind.CommentQuoteOutline } };
        contextMenu.Items.Add(comItem2);

        contextMenu.Items.Add(new Separator());

        if (DateTime.Now - chatData.Time < TimeSpan.FromMinutes(2))
        {
            var comItem3 = new MenuItem
                { Header = "撤回", Icon = new MaterialIcon { Kind = MaterialIconKind.UndoVariant } };
            contextMenu.Items.Add(comItem3);
        }

        var comItem4 = new MenuItem
            { Header = "删除", Icon = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline } };
        contextMenu.Items.Add(comItem4);

        return contextMenu;
    }
}

/// <summary>
/// 头像点击事件参数
/// </summary>
public class GroupHeadClickEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 用户数据
    /// </summary>
    public GroupChatData User { get; }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public GroupHeadClickEventArgs(object sender, RoutedEvent routeEvent, GroupChatData user,
        PointerPressedEventArgs args) : base(routeEvent, sender)
    {
        User = user;
        PointerPressedEventArgs = args;
    }
}