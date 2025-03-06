using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages;
using ChatClient.Tool.Tools;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.Views.ChatPages;

public partial class UserOptionView : UserControl
{
    public UserOptionView()
    {
        InitializeComponent();
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Bitmap bitmap = ((UserOptionsViewModel)this.DataContext!).User.HeadImage;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }
}