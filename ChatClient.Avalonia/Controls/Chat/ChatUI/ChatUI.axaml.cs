using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Platform.Storage.FileIO;
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
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using ChatClient.Media.Audio;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;
using Material.Icons;
using Material.Icons.Avalonia;

namespace ChatClient.Avalonia.Controls.Chat.ChatUI;

public partial class ChatUI : UserControl
{
    private ContextMenu? _contextMenu;

    public ChatUI()
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

    public static readonly StyledProperty<AvaloniaList<ChatData>> MessagesProperty =
        AvaloniaProperty.Register<ChatUI, AvaloniaList<ChatData>>(nameof(Messages),
            defaultValue: new AvaloniaList<ChatData>());

    public AvaloniaList<ChatData> Messages
    {
        get { return GetValue(MessagesProperty); }
        set { SetValue(MessagesProperty, value); }
    }

    #endregion

    #region MessageBoxEvent

    public event EventHandler<MessageBoxShowEventArgs> MessageBoxShow;

    public static readonly RoutedEvent<MessageBoxShowEventArgs> MessageBoxShowEvent =
        RoutedEvent.Register<ChatUI, MessageBoxShowEventArgs>(nameof(MessageBoxShow), RoutingStrategies.Bubble);

    #endregion

    #region HeadClickEvent

    public event EventHandler<FriendHeadClickEventArgs> HeadClick;

    public static readonly RoutedEvent<FriendHeadClickEventArgs> HeadClickEvent =
        RoutedEvent.Register<ChatUI, FriendHeadClickEventArgs>(nameof(HeadClick), RoutingStrategies.Bubble);

    #endregion

    #region NotificationEvent

    public static readonly RoutedEvent<NotificationMessageEventArgs> NotificationEvent =
        RoutedEvent.Register<ChatUI, NotificationMessageEventArgs>(nameof(Notification), RoutingStrategies.Bubble);

    public event EventHandler<NotificationMessageEventArgs> Notification
    {
        add => AddHandler(NotificationEvent, value);
        remove => RemoveHandler(NotificationEvent, value);
    }

    #endregion

    #region SearchMoreCommand

    public static readonly StyledProperty<ICommand> SearchMoreCommandProperty =
        AvaloniaProperty.Register<ChatUI, ICommand>(nameof(SearchMoreCommand));

    public ICommand SearchMoreCommand
    {
        get => GetValue(SearchMoreCommandProperty);
        set => SetValue(SearchMoreCommandProperty, value);
    }

    #endregion

    #region SearchMoreVisible

    public static readonly StyledProperty<bool> SearchMoreVisibleProperty =
        AvaloniaProperty.Register<ChatUI, bool>(nameof(SearchMoreVisible), defaultValue: false);

    public bool SearchMoreVisible
    {
        get => GetValue(SearchMoreVisibleProperty);
        set => SetValue(SearchMoreVisibleProperty, value);
    }

    #endregion

    #region UserImage

    public static readonly StyledProperty<IImage?> UserImageSourceProperty =
        AvaloniaProperty.Register<ChatUI, IImage?>(nameof(UserImageSource));

    public IImage? UserImageSource
    {
        get => GetValue(UserImageSourceProperty);
        set => SetValue(UserImageSourceProperty, value);
    }

    #endregion

    #region FriendImage

    public static readonly StyledProperty<IImage?> FriendImageSourceProperty =
        AvaloniaProperty.Register<ChatUI, IImage?>(nameof(FriendImageSource));

    public IImage? FriendImageSource
    {
        get => GetValue(FriendImageSourceProperty);
        set => SetValue(FriendImageSourceProperty, value);
    }

    #endregion

    #region UnReadMessage

    public static readonly StyledProperty<bool> HaveUnReadMessageProperty =
        AvaloniaProperty.Register<ChatUI, bool>(nameof(HaveUnReadMessage));

    public bool HaveUnReadMessage
    {
        get => GetValue(HaveUnReadMessageProperty);
        set => SetValue(HaveUnReadMessageProperty, value);
    }

    public static readonly StyledProperty<int> UnReadMessageCountProperty =
        AvaloniaProperty.Register<ChatUI, int>(nameof(UnReadMessageCount));

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
        AvaloniaProperty.Register<ChatUI, ICommand>(
            "DeleteMessageCommand");

    public ICommand DeleteMessageCommand
    {
        get => GetValue(DeleteMessageCommandProperty);
        set => SetValue(DeleteMessageCommandProperty, value);
    }

    #endregion

    #region RetractMessageCommand

    public static readonly StyledProperty<ICommand> RetractMessageCommandProperty =
        AvaloniaProperty.Register<ChatUI, ICommand>(
            "RetractMessageCommand");

    public ICommand RetractMessageCommand
    {
        get => GetValue(RetractMessageCommandProperty);
        set => SetValue(RetractMessageCommandProperty, value);
    }

    #endregion

    #region ShareMessageCommand

    public static readonly StyledProperty<ICommand> ShareMessageCommandProperty =
        AvaloniaProperty.Register<ChatUI, ICommand>(
            "ShareMessageCommand");

    public ICommand ShareMessageCommand
    {
        get => GetValue(ShareMessageCommandProperty);
        set => SetValue(ShareMessageCommandProperty, value);
    }

    #endregion

    #region ContextMenuShow

    public static readonly RoutedEvent<RoutedEventArgs> ContextMenuShowEvent =
        RoutedEvent.Register<ChatUI, RoutedEventArgs>(nameof(ContextMenuShow), RoutingStrategies.Bubble);

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
    private void ValueOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            // 最大可偏移量
            double maxOffsetY = MaxOffsetY;
            // 当前偏移量
            double OffsetYOrigion = ChatScroll.Offset.Y;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
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
        if (e.Property == BoundsProperty)
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
                        Cue = new Cue(0)
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
                        Cue = new Cue(1)
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
                    Cue = new Cue(0)
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
                    Cue = new Cue(1)
                }
            }
        };

        // 等待动画完成
        await anim.RunAsync(ChatScroll, AnimationToken.Token);
        isMovingToBottom = false;
    }

    #endregion

    // 左侧好友头像点击
    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            if (sender is Control control)
            {
                if (control.DataContext is ChatData chatData)
                {
                    RaiseEvent(new FriendHeadClickEventArgs(sender, HeadClickEvent, chatData, e));
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

            RaiseEvent(new RoutedEventArgs(ContextMenuShowEvent, this));

            _contextMenu = CreateMenu(chatData);
            _contextMenu.Placement = PlacementMode.Pointer;
            // 获取鼠标位置
            var position = e.GetPosition(this);
            if (position.X > Bounds.Width / 2)
                _contextMenu.PlacementAnchor = PopupAnchor.TopRight;
            else
                _contextMenu.PlacementAnchor = PopupAnchor.TopLeft;

            _contextMenu.Closed += MenuClosed;
            _contextMenu.Open(this);
        }
    }

    private ContextMenu CreateMenu(ChatData chatData)
    {
        ContextMenu contextMenu = new ContextMenu();

        if (chatData.ChatMessages.Count == 1)
        {
            if (chatData.ChatMessages[0].Content is TextMessDto textMessDto)
            {
                var item1 = new MenuItem
                    { Header = "复制", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy } };
                item1.Click += (s, e) =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    topLevel?.Clipboard?.SetTextAsync(textMessDto.Text);
                    RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "已复制到剪贴板",
                        NotificationType.Information));
                };
                contextMenu.Items.Add(item1);
            }
            else if (chatData.ChatMessages[0].Content is ImageMessDto imageMessDto && !imageMessDto.Failed)
            {
                var item1 = new MenuItem
                    { Header = "复制", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy } };
                item1.Click += async (s, e) =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    var file = await topLevel?.StorageProvider.TryGetFileFromPathAsync(imageMessDto.ActualPath);
                    if (file == null) return;
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Files, new List<IStorageItem> { file });
                    topLevel?.Clipboard?.SetDataObjectAsync(dataObject);

                    RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "图片已复制到剪贴板",
                        NotificationType.Information));
                };
                contextMenu.Items.Add(item1);

                var item2 = new MenuItem
                    { Header = "打开图片", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOpenOutline } };
                item2.Click += (s, e) =>
                {
                    if (System.IO.File.Exists(imageMessDto.ActualPath))
                    {
                        Process.Start(new ProcessStartInfo(imageMessDto.ActualPath)
                        {
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Normal
                        });
                    }
                    else
                        RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "图片不存在",
                            NotificationType.Information));
                };
                contextMenu.Items.Add(item2);

                var item3 = new MenuItem
                    { Header = "打开所在文件夹", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOutline } };
                item3.Click += (s, e) =>
                {
                    if (System.IO.File.Exists(imageMessDto.ActualPath))
                    {
                        var argument = $"/select,\"{imageMessDto.ActualPath}\"";
                        Process.Start(new ProcessStartInfo("explorer.exe", argument)
                        {
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Normal
                        });
                    }
                    else
                        RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "图片不存在",
                            NotificationType.Information));
                };
                contextMenu.Items.Add(item3);

                var item4 = new MenuItem
                    { Header = "另存为", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentSaveMoveOutline } };
                item4.Click += (sender, args) => { FileRestoreCommand.Execute(imageMessDto); };
                contextMenu.Items.Add(item4);
            }
            else if (chatData.ChatMessages[0].Content is VoiceMessDto voiceMessDto && !voiceMessDto.Failed)
            {
                var item1 = new MenuItem
                    { Header = "播放", Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy } };
                item1.Click += async (s, e) =>
                {
                    if (voiceMessDto.IsPlaying)
                    {
                        voiceMessDto.AudioPlayer?.Stop();
                        voiceMessDto.IsPlaying = false;
                        voiceMessDto.AudioPlayer = null;
                    }
                    else if (voiceMessDto.AudioData != null)
                    {
                        using (var audioPlayer = new AudioPlayer())
                        {
                            voiceMessDto.IsPlaying = true;
                            voiceMessDto.AudioPlayer = audioPlayer;
                            audioPlayer.LoadFromMemory(voiceMessDto.AudioData);
                            await audioPlayer.PlayToEndAsync();
                            voiceMessDto.IsPlaying = false;
                            voiceMessDto.AudioPlayer = null;
                        }
                    }
                };
                contextMenu.Items.Add(item1);

                var item2 = new MenuItem
                    { Header = "打开所在文件夹", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOutline } };
                item2.Click += (s, e) =>
                {
                    if (System.IO.File.Exists(voiceMessDto.ActualPath))
                    {
                        var argument = $"/select,\"{voiceMessDto.ActualPath}\"";
                        Process.Start(new ProcessStartInfo("explorer.exe", argument)
                        {
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Normal
                        });
                    }
                    else
                        RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "语音不存在",
                            NotificationType.Information));
                };
                contextMenu.Items.Add(item2);
            }
            else if (chatData.ChatMessages[0].Content is FileMessDto fileMessDto)
            {
                if (fileMessDto.IsSuccess)
                {
                    var item1 = new MenuItem
                        { Header = "打开文件", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOpenOutline } };
                    item1.Click += (s, e) =>
                    {
                        if (System.IO.File.Exists(fileMessDto.TargetFilePath))
                        {
                            Process.Start(new ProcessStartInfo(fileMessDto.TargetFilePath)
                            {
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Normal
                            });
                        }
                        else
                        {
                            fileMessDto.IsSuccess = false;
                            RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "文件不存在",
                                NotificationType.Information));
                        }
                    };
                    contextMenu.Items.Add(item1);

                    var item2 = new MenuItem
                        { Header = "打开所在文件夹", Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOutline } };
                    item2.Click += (s, e) =>
                    {
                        if (System.IO.File.Exists(fileMessDto.TargetFilePath))
                        {
                            var argument = $"/select,\"{fileMessDto.TargetFilePath}\"";
                            System.Diagnostics.Process.Start(
                                new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
                                {
                                    UseShellExecute = true
                                });
                        }
                        else
                        {
                            fileMessDto.IsSuccess = false;
                            RaiseEvent(new NotificationMessageEventArgs(this, NotificationEvent, "文件不存在",
                                NotificationType.Information));
                        }
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
        }

        // 文件和复合消息暂不支持转发
        if (chatData.ChatMessages.Count == 1 &&
            (chatData.ChatMessages[0].Content is not FileMessDto ||
             chatData.ChatMessages[0].Content is ImageMessDto imageMess && !imageMess.Failed ||
             chatData.ChatMessages[0].Content is VoiceMessDto voiceMess && !voiceMess.Failed))
        {
            var comItem1 = new MenuItem
                { Header = "转发", Icon = new MaterialIcon { Kind = MaterialIconKind.Forwardburger } };
            comItem1.Click += (sender, args) => { ShareMessageCommand?.Execute(chatData.ChatMessages[0].Content); };
            contextMenu.Items.Add(comItem1);
        }

        var comItem2 = new MenuItem
            { Header = "引用", Icon = new MaterialIcon { Kind = MaterialIconKind.CommentQuoteOutline } };
        contextMenu.Items.Add(comItem2);

        contextMenu.Items.Add(new Separator());

        if (DateTime.Now - chatData.Time < TimeSpan.FromMinutes(2) && chatData.IsUser)
        {
            var comItem3 = new MenuItem
                { Header = "撤回", Icon = new MaterialIcon { Kind = MaterialIconKind.UndoVariant } };
            comItem3.Click += (sender, args) => { RetractMessageCommand?.Execute(chatData); };
            contextMenu.Items.Add(comItem3);
        }

        var comItem4 = new MenuItem
            { Header = "删除", Icon = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline } };
        comItem4.Click += (sender, args) => { DeleteMessageCommand?.Execute(chatData); };
        contextMenu.Items.Add(comItem4);

        return contextMenu;
    }

    private void MenuClosed(object sender, RoutedEventArgs args)
    {
        if (sender is ContextMenu contextMenu)
        {
            contextMenu.DataContext = null;
            contextMenu.Items.Clear();
            contextMenu.Closed -= MenuClosed;
        }
    }

    private void ChatMessageView_OnMessageBoxShow(object? sender, MessageBoxShowEventArgs e)
    {
        e.PointerPressedEventArgs.Source = sender;
        RaiseEvent(new MessageBoxShowEventArgs(sender, MessageBoxShowEvent, e.PointerPressedEventArgs,
            e.CardMessDto));
    }

    public void CloseMenu()
    {
        if (_contextMenu != null)
        {
            _contextMenu.Close();
            _contextMenu = null;
        }
    }
}

/// <summary>
/// 头像点击事件参数
/// </summary>
public class FriendHeadClickEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 用户数据
    /// </summary>
    public ChatData User { get; }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public FriendHeadClickEventArgs(object sender, RoutedEvent routeEvent, ChatData user, PointerPressedEventArgs args)
        : base(routeEvent, sender)
    {
        User = user;
        PointerPressedEventArgs = args;
    }
}

public class NotificationMessageEventArgs : RoutedEventArgs
{
    public string Message { get; }

    public NotificationType Type { get; }

    public NotificationMessageEventArgs(object sender, RoutedEvent routeEvent, string message, NotificationType type)
        : base(routeEvent, sender)
    {
        Message = message;
        Type = type;
    }
}

public class MessageBoxShowEventArgs : RoutedEventArgs
{
    public CardMessDto CardMessDto { get; }

    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public MessageBoxShowEventArgs(object sender, RoutedEvent routedEvent, PointerPressedEventArgs pressArgs,
        CardMessDto cardMessDto)
        : base(routedEvent, sender)
    {
        CardMessDto = cardMessDto;
        PointerPressedEventArgs = pressArgs;
    }
}