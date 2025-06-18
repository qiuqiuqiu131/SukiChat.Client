using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class SukiChatDialogWindow : SukiWindow, IDialogWindow
{
    public static readonly StyledProperty<IThemeStyle> ThemeStyleProperty =
        AvaloniaProperty.Register<SukiDialogWindow, IThemeStyle>(
            "ThemeStyle");

    public IThemeStyle ThemeStyle
    {
        get => GetValue(ThemeStyleProperty);
        set => SetValue(ThemeStyleProperty, value);
    }

    public SukiChatDialogWindow(IThemeStyle themeStyle)
    {
        InitializeComponent();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        ThemeStyle = themeStyle;

        CanMaximize = true;
        CanMinimize = true;
        CanResize = true;
        IsTitleBarVisible = false;
        IsMenuVisible = false;
        TitleBarAnimationEnabled = false;

        BackgroundAnimationEnabled = true;
        BackgroundTransitionsEnabled = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public IDialogResult Result { get; set; }

    public IChatDialogID? ChatDialogId => (Content as Control)?.DataContext as IChatDialogID;

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