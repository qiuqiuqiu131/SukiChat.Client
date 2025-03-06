using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.Views.UserControls;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatRightBottomPanelView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<object>> InputMessagesProperty
        = AvaloniaProperty.Register<ChatRightBottomPanelView, AvaloniaList<object>>(nameof(InputMessages));

    public AvaloniaList<object> InputMessages
    {
        get => GetValue(InputMessagesProperty);
        set => SetValue(InputMessagesProperty, value);
    }

    private ItemCollection _itemCollection;

    public ChatRightBottomPanelView()
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
                            control.GotFocus += ControlOnGotFocus;

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
                }

                removeItems.Add(control);
            }

            foreach (var item in removeItems)
                _itemCollection.Remove(item);

            if (InputMessages.Count == 0)
                InputMessages.Add(string.Empty);
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
        if (sender is TextBox textBox)
        {
            // 更新后台输入消息
            int index = _itemCollection.IndexOf(textBox);
            InputMessages[index] = textBox.Text;
            e.Handled = true;
        }
    }

    private void InputItems_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var textBox = (TextBox)_itemCollection.Last()!;
        textBox.Focus();
        ScrollViewer.ScrollToEnd();
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
}