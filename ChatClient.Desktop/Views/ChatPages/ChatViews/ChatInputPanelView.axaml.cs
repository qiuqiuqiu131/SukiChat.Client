using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.Views.UserControls;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatInputPanelView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<object>> InputMessagesProperty
        = AvaloniaProperty.Register<ChatInputPanelView, AvaloniaList<object>>(nameof(InputMessages));

    public AvaloniaList<object> InputMessages
    {
        get => GetValue(InputMessagesProperty);
        set => SetValue(InputMessagesProperty, value);
    }

    private ItemCollection _itemCollection;

    public ChatInputPanelView()
    {
        InitializeComponent();
        _itemCollection = InputItems.Items;
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
                        textBox.KeyDown -= ControlOnKeyDown;
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
                            control.GotFocus += ControlOnGotFocus;
                            control.KeyDown += ControlOnKeyDown;

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
                control.KeyDown += ControlOnKeyDown;

                _itemCollection.Add(control);
            }
            else if (e.NewItems[0] is Bitmap bitmap)
            {
                var control = (Image)DataTemplates[1].Build(bitmap)!;
                control.DataContext = bitmap;

                _itemCollection.Add(control);
                InputMessages.Add(string.Empty);
            }

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
                    textBox.KeyDown -= ControlOnKeyDown;
                }

                removeItems.Add(control);
            }

            foreach (var item in removeItems)
                _itemCollection.Remove(item);

            if (InputMessages.Count == 0)
                InputMessages.Add(string.Empty);
        }
    }

    #region TextBoxEvent

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
        // if (sender is TextBox textBox)
        // {
        //     var clipboard = TopLevel.GetTopLevel(textBox)?.Clipboard;
        //     if (clipboard != null)
        //     {
        //         var datas = await clipboard.GetFormatsAsync();
        //         foreach (var data in datas)
        //         {
        //             var obj = await clipboard.GetDataAsync(data);
        //         }
        //     }
        // }
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
                ScrollViewer.ScrollToEnd();

                e.Handled = true;
            }
            else if (e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
            {
                var textBox = (TextBox)_itemCollection.Last()!;
                textBox.Focus();
                if (textBox.ContextFlyout != null)
                    textBox.ContextFlyout.ShowAt(textBox);

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
        //EmojiView.IsOpen = false;
        var textBox = (TextBox)_itemCollection.Last()!;
        textBox.Text += e.Emoji;
        textBox.Focus();
        ScrollViewer.ScrollToEnd();
    }

    private void ScrollViewer_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ChatInputPanelViewModel chatInputPanelViewModel)
            {
                if (chatInputPanelViewModel.SendMessageCommand.CanExecute())
                    chatInputPanelViewModel.SendMessageCommand.Execute();
            }
        }
    }
}