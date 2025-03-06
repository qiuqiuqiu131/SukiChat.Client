using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

        BackgroundAnimationEnabled = themeStyle.CurrentThemeStyle.AnimationsEnabled;
        BackgroundStyle = themeStyle.CurrentThemeStyle.BackgroundStyle;
        BackgroundTransitionsEnabled = themeStyle.CurrentThemeStyle.TransitionsEnabled;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public IDialogResult Result { get; set; }
}