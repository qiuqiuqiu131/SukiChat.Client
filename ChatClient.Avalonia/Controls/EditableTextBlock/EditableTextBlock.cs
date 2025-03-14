using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace ChatClient.Avalonia.Controls.EditableTextBlock;

public class EditableTextBlock : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<EditableTextBlock, string?>(
            nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> DefaultTextProperty =
        AvaloniaProperty.Register<EditableTextBlock, string>(
            nameof(DefaultText));

    public string DefaultText
    {
        get => GetValue(DefaultTextProperty);
        set => SetValue(DefaultTextProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<EditableTextBlock, bool>(
        nameof(IsEditing), defaultValue: false);

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<EditableTextBlock, double>(
            nameof(FontSize), defaultValue: 12);

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    private TextBlock _textBlock;
    private TextBlock _defaultTextBox;
    private TextBox _textBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBlock = e.NameScope.Get<TextBlock>("PART_TextBlock");
        _defaultTextBox = e.NameScope.Get<TextBlock>("PART_DefaultTextBlock");
        _textBox = e.NameScope.Get<TextBox>("PART_TextBox");

        _textBlock.PointerPressed += TextBlockOnPointerPressed;
        _defaultTextBox.PointerPressed += TextBlockOnPointerPressed;
        _textBox.LostFocus += TextBoxOnLostFocus;
        _textBox.KeyDown += TextBoxOnKeyDown;
    }

    private void TextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            IsEditing = false;
    }

    private void TextBoxOnLostFocus(object? sender, RoutedEventArgs e)
    {
        IsEditing = false;
    }

    private async void TextBlockOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsEditing) return;

        IsEditing = true;
        _textBox.Focus();
        _textBox.SelectAll();

        e.Handled = true;
    }
}