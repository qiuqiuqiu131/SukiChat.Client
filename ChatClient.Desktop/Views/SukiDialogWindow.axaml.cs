using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Tool.ManagerInterface;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class SukiDialogWindow : SukiWindow, IDialogWindow
{
    public static readonly StyledProperty<IThemeStyle> ThemeStyleProperty =
        AvaloniaProperty.Register<SukiDialogWindow, IThemeStyle>(
            "ThemeStyle");

    public IThemeStyle ThemeStyle
    {
        get => GetValue(ThemeStyleProperty);
        set => SetValue(ThemeStyleProperty, value);
    }

    public SukiDialogWindow(IThemeStyle themeStyle)
    {
        InitializeComponent();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        ThemeStyle = themeStyle;

        CanMaximize = false;
        CanMinimize = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        IsTitleBarVisible = false;
        IsMenuVisible = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        TitleBarAnimationEnabled = false;

        BackgroundAnimationEnabled = true;
        BackgroundTransitionsEnabled = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public IDialogResult Result { get; set; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Opacity = 1;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled) return;
        FocusManager?.ClearFocus();
    }
}