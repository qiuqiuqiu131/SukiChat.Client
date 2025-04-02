using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChatClient.Avalonia.Controls.SearchBox;

public class SearchBox : UserControl
{
    public static readonly StyledProperty<bool> IsFocusProperty = AvaloniaProperty.Register<SearchBox, bool>(
        nameof(IsFocus));

    public bool IsFocus
    {
        get => GetValue(IsFocusProperty);
        set => SetValue(IsFocusProperty, value);
    }

    public static readonly StyledProperty<string> WaterMaskProperty = AvaloniaProperty.Register<SearchBox, string>(
        nameof(WaterMask));

    public string WaterMask
    {
        get => GetValue(WaterMaskProperty);
        set => SetValue(WaterMaskProperty, value);
    }

    public static readonly StyledProperty<string> SearchTextProperty =
        AvaloniaProperty.Register<SearchBox, string>(nameof(SearchText));

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<SearchBox, TextChangedEventArgs>(
            nameof(TextChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<TextChangedEventArgs> TextChanged
    {
        add => AddHandler(TextChangedEvent, value);
        remove => RemoveHandler(TextChangedEvent, value);
    }

    private TextBox _textBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _textBox = e.NameScope.Get<TextBox>("PART_TextBox");
        _textBox.TextChanged += TextBoxOnTextChanged;
    }

    private void TextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            RaiseEvent(new TextChangedEventArgs(TextChangedEvent, this));
        }
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        _textBox.Focus();
        IsFocus = true;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        IsFocus = false;
    }
}