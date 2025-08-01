using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.UserControls.ChatUI;
using ChatClient.Tool.Data.ChatMessage;
using Material.Icons;
using Material.Icons.Avalonia;

namespace ChatClient.Desktop.Views.UserControls.GroupChatUI;

public partial class GroupChatUI : UserControl
{
    private ContextMenu? _contextMenu;
    private Control? ChatScrollContent;

    public GroupChatUI()
    {
        InitializeComponent();

        ChatScrollContent = ChatScrollViewer.Content as Control ??
                            throw new InvalidOperationException("ChatScroll.Content must be a Control");
        ChatScrollViewer.PropertyChanged += ChatScrollOnPropertyChanged;
        ChatScrollViewer.GotFocus += (sender, args) =>
        {
            ChatScrollViewer.Offset = new Vector(0, ChatScrollViewer.CurrentPos);
        };

        // 捕获鼠标滚动事件
        var wheelEvent = Observable.FromEventPattern<PointerWheelEventArgs>(
            h => ChatScrollContent.PointerWheelChanged += h,
            h => ChatScrollContent.PointerWheelChanged -= h);

        wheelEvent.Subscribe(arg => arg.EventArgs.Handled = true);

        wheelEvent.Select(arg => -arg.EventArgs.Delta.Y * 150)
            .Subscribe(d => ChatScrollViewer.MoveUp(d));

        IC.PropertyChanged += IC_OnPropertyChanged;
    }

    #region Property

    #region HeadClickEvent

    public event EventHandler<GroupHeadClickEventArgs> HeadClick;

    public static readonly RoutedEvent<GroupHeadClickEventArgs> HeadClickEvent =
        RoutedEvent.Register<GroupChatUI, GroupHeadClickEventArgs>(nameof(HeadClick), RoutingStrategies.Bubble);

    #endregion

    #region MessageBoxEvent

    public event EventHandler<MessageBoxShowEventArgs> MessageBoxShow;

    public static readonly RoutedEvent<MessageBoxShowEventArgs> MessageBoxShowEvent =
        RoutedEvent.Register<GroupChatUI, MessageBoxShowEventArgs>(nameof(MessageBoxShow), RoutingStrategies.Bubble);

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

    private void IC_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ItemsControl.ItemsSourceProperty)
        {
            if (e.OldValue is AvaloniaList<ChatData> oldMessages)
                oldMessages.CollectionChanged -= ValueOnCollectionChanged;

            if (e.NewValue is AvaloniaList<ChatData> newMessages)
                newMessages.CollectionChanged += ValueOnCollectionChanged;

            OnItemsSourceChanged();
        }
    }

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
        double maxOffsetY = ChatScrollViewer.MaxOffsetY; // 最大可偏移量
        double OffsetYOrigion = ChatScrollViewer.Offset.Y; // 当前偏移量
        Dispatcher.UIThread.Post(async () =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex == 0)
                {
                    // -- 加载历史聊天记录 --//
                    ChatScrollViewer.UnLock();
                }
                else if (ChatScrollViewer.IsToMoving
                         || Math.Abs(OffsetYOrigion - maxOffsetY) < 50 // 靠近底部且不是扩展聊天记录
                         || e.NewItems?[0] is GroupChatData chatData && chatData.IsUser) // 是自己发送的消息
                {
                    ChatScrollViewer.ScrollToBottom();
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
            double maxOffsetY = ChatScrollViewer.MaxOffsetY;
            // 当前偏移量
            double OffsetYOrigion = ChatScrollViewer.Offset.Y;

            if (Math.Abs(OffsetYOrigion - maxOffsetY) < 5)
                ChatScrollViewer.Offset = new Vector(ChatScrollViewer.Offset.X, maxOffsetY + 5);
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
            if (Math.Abs(ChatScrollViewer.Offset.Y - ChatScrollViewer.MaxOffsetY) < 10)
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
    private void Button_OnClick(object? sender, RoutedEventArgs e) => ChatScrollViewer.MoveToBottom();

    /// <summary>
    /// 查看更多聊天记录按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 锁定ScrollViewer滚动
        ChatScrollViewer.Lock();
        ((ChatGroupPanelViewModel)DataContext!).SearchMoreCommand.Execute();
        await Task.Delay(1500).ConfigureAwait(true);
        ChatScrollViewer.UnLock();
    }


    /// <summary>
    /// 当ChatUI绑定的ItemsSource发生变化时调用，当聊天对象发生变化时调用，将聊天框滚到最底部
    /// </summary>
    private void OnItemsSourceChanged()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (ChatScrollViewer != null)
                ChatScrollViewer.Opacity = 0;
        });
        Dispatcher.UIThread.Post(() =>
        {
            ChatScrollViewer.Opacity = 1;
            ChatScrollViewer.ScrollToBottom();
        }, DispatcherPriority.Background);
    }

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
        if (sender is Control control && control.DataContext is GroupChatData chatData &&
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

    private void MenuClosed(object sender, RoutedEventArgs args)
    {
        if (sender is ContextMenu contextMenu)
        {
            contextMenu.DataContext = null;
            contextMenu.Items.Clear();
            contextMenu.Closed -= MenuClosed;
        }
    }

    private ContextMenu CreateMenu(GroupChatData chatData)
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
                    if (!string.IsNullOrEmpty(imageMessDto.ActualPath) &&
                        System.IO.File.Exists(imageMessDto.ActualPath))
                    {
                        var argument = $"/select,\"{imageMessDto.ActualPath}\"";
                        Process.Start(new ProcessStartInfo("explorer.exe", argument)
                        {
                            UseShellExecute = true
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
                    try
                    {
                        if (voiceMessDto.IsPlaying)
                        {
                            voiceMessDto.AudioPlayer?.StopAsync();
                            voiceMessDto.IsPlaying = false;
                            voiceMessDto.AudioPlayer = null;
                        }
                        else if (voiceMessDto.AudioData != null && voiceMessDto.AudioPlayerFactory != null)
                        {
                            using (var audioPlayer = voiceMessDto.AudioPlayerFactory.Create())
                            {
                                voiceMessDto.IsPlaying = true;
                                voiceMessDto.AudioPlayer = audioPlayer;
                                audioPlayer.LoadFromMemory(voiceMessDto.AudioData);
                                await audioPlayer.PlayToEndAsync();
                                voiceMessDto.IsPlaying = false;
                                voiceMessDto.AudioPlayer = null;
                            }
                        }
                    }
                    catch
                    {
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
                        if (!string.IsNullOrEmpty(fileMessDto.TargetFilePath) &&
                            System.IO.File.Exists(fileMessDto.TargetFilePath))
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
                        if (!string.IsNullOrEmpty(fileMessDto.TargetFilePath) &&
                            System.IO.File.Exists(fileMessDto.TargetFilePath))
                        {
                            var argument = $"/select,\"{fileMessDto.TargetFilePath}\"";
                            Process.Start(
                                new ProcessStartInfo("explorer.exe", argument)
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
        if (chatData.ChatMessages[0].Content is not FileMessDto || chatData.ChatMessages.Count > 1)
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

    private void GroupChatMessageView_OnMessageBoxShow(object sender, MessageBoxShowEventArgs e)
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