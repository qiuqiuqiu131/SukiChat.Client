using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Events;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.Input;

public partial class ChatInputPanelView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<object>> InputMessagesProperty
        = AvaloniaProperty.Register<ChatInputPanelView, AvaloniaList<object>>(nameof(InputMessages));

    public AvaloniaList<object> InputMessages
    {
        get => GetValue(InputMessagesProperty);
        set => SetValue(InputMessagesProperty, value);
    }

    public static readonly StyledProperty<ICommand> SendFileCommandProperty =
        AvaloniaProperty.Register<ChatInputPanelView, ICommand>(
            "SendFileCommand");

    public ICommand SendFileCommand
    {
        get => GetValue(SendFileCommandProperty);
        set => SetValue(SendFileCommandProperty, value);
    }

    private ItemCollection _itemCollection;
    private readonly IEventAggregator eventAggregator;

    public ChatInputPanelView()
    {
        InitializeComponent();
        _itemCollection = InputItems.Items;
        ScrollViewer.AddHandler(DragDrop.DropEvent, DropEventHandler);

        eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<NewMenuShow>().Subscribe(() =>
        {
            foreach (var item in _itemCollection)
            {
                if (item is TextBox textBox)
                    textBox.ContextMenu?.Close();
            }
        });
    }

    /// <summary>
    /// 属性更改时调用，当聊天对象发生变化时，更新输入消息集合
    /// </summary>
    /// <param name="change"></param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == InputMessagesProperty)
        {
            if (change.NewValue is AvaloniaList<object> newInputMessages)
            {
                newInputMessages.CollectionChanged += InputMessagesOnCollectionChanged;

                // 初始化输入消息
                // 清空原有的输入消息
                foreach (var item in _itemCollection)
                    if (item is TextBox textBox)
                    {
                        textBox.TextChanged -= ControlOnTextChanged;
                        textBox.GotFocus -= ControlOnGotFocus;
                        textBox.PastingFromClipboard -= ControlOnPastingFromClipboard;
                        textBox.PointerPressed -= ControlOnPointerPressed;
                        textBox.RemoveHandler(KeyDownEvent, ControlOnKeyDown);
                    }

                _itemCollection.Clear();

                // 初始化输入消息
                if (newInputMessages.Count == 0)
                {
                    InputMessages.Add(string.Empty);
                }
                else
                {
                    foreach (var message in newInputMessages)
                    {
                        if (message is string text)
                        {
                            var control = (TextBox)DataTemplates[0].Build(text)!;
                            control.DataContext = text;
                            control.TextChanged += ControlOnTextChanged;
                            control.PastingFromClipboard += ControlOnPastingFromClipboard;
                            control.PointerPressed += ControlOnPointerPressed;
                            control.GotFocus += ControlOnGotFocus;
                            control.AddHandler(KeyDownEvent, ControlOnKeyDown, RoutingStrategies.Tunnel);

                            _itemCollection.Add(control);
                        }
                        else if (message is Bitmap bitmap)
                        {
                            var control = (Image)DataTemplates[1].Build(bitmap)!;
                            control.DataContext = bitmap;
                            _itemCollection.Add(control);
                        }
                    }

                    if (newInputMessages.Last() is Bitmap)
                        InputMessages.Add(string.Empty);
                }
            }

            if (change.OldValue is AvaloniaList<object> oldInputMessages)
            {
                oldInputMessages.CollectionChanged -= InputMessagesOnCollectionChanged;
            }
        }
    }

    // 当输入消息集合发生变化时调用
    private void InputMessagesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // 后台添加消息
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems[0] is string text)
                {
                    var control = (TextBox)DataTemplates[0].Build(text)!;
                    control.DataContext = text;

                    control.TextChanged += ControlOnTextChanged;
                    control.GotFocus += ControlOnGotFocus;
                    control.PastingFromClipboard += ControlOnPastingFromClipboard;
                    control.PointerPressed += ControlOnPointerPressed;
                    control.AddHandler(KeyDownEvent, ControlOnKeyDown, RoutingStrategies.Tunnel);

                    _itemCollection.Add(control);
                }
                else if (e.NewItems[0] is Bitmap bitmap)
                {
                    var control = (Image)DataTemplates[1].Build(bitmap)!;
                    control.DataContext = bitmap;

                    _itemCollection.Add(control);
                    InputMessages.Add(string.Empty);
                }

                var con = _itemCollection.Last() as Control;
                if (con != null)
                    con.Focus();
                ScrollViewer.ScrollToEnd();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                List<object> removeItems = new();
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    var control = _itemCollection[i + e.OldStartingIndex];
                    if (control is TextBox textBox)
                    {
                        textBox.TextChanged -= ControlOnTextChanged;
                        textBox.GotFocus -= ControlOnGotFocus;
                        textBox.PastingFromClipboard -= ControlOnPastingFromClipboard;
                        textBox.PointerPressed -= ControlOnPointerPressed;
                        textBox.RemoveHandler(KeyDownEvent, ControlOnKeyDown);
                    }

                    if (e.OldItems[i] is Bitmap bitmap)
                        bitmap.Dispose();

                    removeItems.Add(control);
                }

                foreach (var item in removeItems)
                    _itemCollection.Remove(item);

                if (InputMessages.Count == 0)
                {
                    // 消息清空时，Focus选中输入框
                    InputMessages.Add(string.Empty);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemCollection.Clear();
                InputMessages.Add(string.Empty);
            }
        });
    }

    #region TextBoxEvent

    private async void DropEventHandler(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var storageItems = e.Data.GetFiles();
            if (storageItems != null)
            {
                var imageFiles = storageItems.Where(item => item is IStorageFile file &&
                                                            (file.Name.EndsWith(".png",
                                                                 StringComparison.OrdinalIgnoreCase) ||
                                                             file.Name.EndsWith(".jpg",
                                                                 StringComparison.OrdinalIgnoreCase) ||
                                                             file.Name.EndsWith(".jpeg",
                                                                 StringComparison.OrdinalIgnoreCase) ||
                                                             file.Name.EndsWith(".bmp",
                                                                 StringComparison.OrdinalIgnoreCase)))
                    .Cast<IStorageFile>();
                if (imageFiles.Count() != 0)
                    foreach (var imageFile in imageFiles)
                    {
                        Bitmap bitmap;
                        using (var stream = await imageFile.OpenReadAsync())
                            bitmap = new Bitmap(stream);
                        if (InputMessages.Last() is string str && string.IsNullOrEmpty(str))
                        {
                            var index = InputMessages.Count - 1;
                            InputMessages?.Add(bitmap);
                            InputMessages?.RemoveAt(index);
                        }
                        else
                            InputMessages?.Add(bitmap);
                    }
                else
                {
                    if (storageItems.Count() >= 2)
                    {
                        var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                        eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                        {
                            Message = "暂不支持多文件发送",
                            Type = NotificationType.Warning
                        });
                    }
                    else if (storageItems.Count() == 1)
                    {
                        var file = storageItems.FirstOrDefault();
                        if (file != null)
                        {
                            SendFileCommand?.Execute(file.Path.LocalPath);
                        }
                    }
                }
            }
        }
    }

    private void ControlOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// 接受输入框的键盘事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ControlOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (e.Key == Key.Enter && (e.KeyModifiers == KeyModifiers.Shift || e.KeyModifiers == KeyModifiers.Control))
            {
                int selectionStart = textBox.SelectionStart < textBox.SelectionEnd
                    ? textBox.SelectionStart
                    : textBox.SelectionEnd;
                int selectionEnd = textBox.SelectionStart < textBox.SelectionEnd
                    ? textBox.SelectionEnd
                    : textBox.SelectionStart;
                ;

                // 如果有选中的文本，则替换选中的文本
                if (selectionStart != selectionEnd)
                {
                    string orgionText = textBox.Text;
                    // 替换选中的文本为换行符
                    textBox.Text = orgionText.Remove(selectionStart, selectionEnd - selectionStart)
                        .Insert(selectionStart, Environment.NewLine);

                    // 将光标移动到新插入的换行符之后
                    textBox.SelectionStart = selectionStart + Environment.NewLine.Length;
                    textBox.SelectionEnd = selectionStart + Environment.NewLine.Length;
                }
                else
                {
                    // 如果没有选中文本，则在光标位置插入换行符
                    int caretIndex = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Insert(caretIndex, Environment.NewLine);
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (DataContext is ChatInputPanelViewModel chatInputPanelViewModel)
                {
                    if (chatInputPanelViewModel.SendMessageCommand.CanExecute())
                        chatInputPanelViewModel.SendMessageCommand.Execute();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    // 获取索引
                    var index = _itemCollection.IndexOf(textBox);
                    if (index != 0)
                    {
                        var previousItem = _itemCollection[index - 1];
                        if (previousItem is Image image)
                        {
                            InputMessages.Remove(image.DataContext);
                        }
                        else if (previousItem is TextBox preTextBox)
                        {
                            InputMessages.RemoveAt(index);
                            preTextBox.Focus();
                            preTextBox.CaretIndex = preTextBox.Text?.Length ?? 0;
                        }
                    }

                    e.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// 接受输入框的粘贴事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void ControlOnPastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var clipboard = TopLevel.GetTopLevel(textBox)?.Clipboard;
            if (clipboard != null)
            {
                var files = await clipboard.GetDataAsync(DataFormats.Files);
                if (files is IEnumerable<IStorageItem> storageItems)
                {
                    var imageFiles = storageItems.Where(item => item is IStorageFile file &&
                                                                (file.Name.EndsWith(".png",
                                                                     StringComparison.OrdinalIgnoreCase) ||
                                                                 file.Name.EndsWith(".jpg",
                                                                     StringComparison.OrdinalIgnoreCase) ||
                                                                 file.Name.EndsWith(".jpeg",
                                                                     StringComparison.OrdinalIgnoreCase) ||
                                                                 file.Name.EndsWith(".bmp",
                                                                     StringComparison.OrdinalIgnoreCase)))
                        .Cast<IStorageFile>();
                    if (imageFiles.Count() != 0)
                        foreach (var imageFile in imageFiles)
                        {
                            Bitmap bitmap;
                            using (var stream = await imageFile.OpenReadAsync())
                                bitmap = new Bitmap(stream);
                            if (InputMessages.Last() is string str && string.IsNullOrEmpty(str))
                            {
                                var index = InputMessages.Count - 1;
                                InputMessages?.Add(bitmap);
                                InputMessages?.RemoveAt(index);
                            }
                            else
                                InputMessages?.Add(bitmap);
                        }
                    else
                    {
                        if (storageItems.Count() >= 2)
                        {
                            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                            {
                                Message = "暂不支持多文件发送",
                                Type = NotificationType.Warning
                            });
                        }
                        else if (storageItems.Count() == 1)
                        {
                            var file = storageItems.FirstOrDefault();
                            if (file != null)
                            {
                                SendFileCommand?.Execute(file.Path.LocalPath);
                            }
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// 接受输入框的焦点事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ControlOnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectionStart = textBox.Text?.Length ?? 0;
            textBox.SelectionEnd = textBox.Text?.Length ?? 0;
        }
    }

    /// <summary>
    /// 接受输入框的输入事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ControlOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (InputMessages == null) return;
        if (sender is TextBox textBox)
        {
            // 更新后台输入消息
            int index = _itemCollection.IndexOf(textBox);
            InputMessages[index] = textBox.Text;
            e.Handled = true;
        }
    }

    #endregion

    private void InputItems_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            if (e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            {
                var textBox = (TextBox)_itemCollection.Last()!;
                textBox.Focus();
                textBox.SelectionStart = textBox.Text?.Length ?? 0;
                textBox.SelectionEnd = textBox.Text?.Length ?? 0;

                ScrollViewer.ScrollToEnd();

                e.Handled = true;
            }
            else if (e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
            {
                var textBox = (TextBox)_itemCollection.Last()!;
                textBox.Focus();
                textBox.SelectionStart = textBox.Text?.Length ?? 0;
                textBox.SelectionEnd = textBox.Text?.Length ?? 0;

                if (control.ContextMenu != null)
                {
                    var item = (MenuItem)control.ContextMenu.Items[3];
                    if (string.IsNullOrEmpty(textBox.Text))
                        item.IsEnabled = false;
                    else
                        item.IsEnabled = true;
                    control.ContextMenu.Open();
                }

                e.Handled = true;
            }
        }
    }

    private void SelectEmojis(object? sender, RoutedEventArgs e)
    {
        EmojiView.IsOpen = !EmojiView.IsOpen;
    }

    // 选择emoji表情
    private void EmojiPickerView_OnEmojiSelected(object? sender, EmojiSelectedEventArgs e)
    {
        var textBox = (TextBox)_itemCollection.Last()!;
        textBox.Text += e.Emoji;
        textBox.Focus();
        ScrollViewer.ScrollToEnd();
    }

    #region ScrollViewerMenu

    private void TextBoxContextFlyoutPasteItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)_itemCollection.Last()!;
        textBox.Paste();
    }

    private void TextBoxContextFlyoutSelectAllItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)_itemCollection.Last()!;
        textBox.SelectAll();
    }

    #endregion
}