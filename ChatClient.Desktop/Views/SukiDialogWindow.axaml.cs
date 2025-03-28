using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ChatClient.Tool.ManagerInterface;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class SukiDialogWindow : SukiWindow, IDialogWindow
{
    public SukiDialogWindow(IThemeStyle themeStyle)
    {
        InitializeComponent();

        CanMaximize = false;
        CanMinimize = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        IsTitleBarVisible = false;
        IsMenuVisible = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        TitleBarAnimationEnabled = false;

        // Icon = new WindowIcon(Environment.CurrentDirectory + "/Assets/DefaultHead.ico");

        BackgroundAnimationEnabled = themeStyle.CurrentThemeStyle.AnimationsEnabled;
        BackgroundStyle = themeStyle.CurrentThemeStyle.BackgroundStyle;
        BackgroundTransitionsEnabled = themeStyle.CurrentThemeStyle.TransitionsEnabled;
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