using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.Login;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views.Login;

public partial class LoginWindowView : SukiWindow
{
    public LoginWindowView()
    {
        InitializeComponent();

        // 字体渲染模式：Alias - 锯齿、Antialias - 抗锯齿、SubpixelAntialias - 次像素抗锯齿
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);

        // 如果发现图片呈现不清晰，应该可以通过该配置优化渲染：位图插值模式
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        // 如果发现形状边缘有锯齿，应该可以通过该配置优化渲染：边缘渲染模式
        RenderOptions.SetEdgeMode(this, EdgeMode.Antialias);
    }

    private SukiDialogHost? _dialogHost;

    protected override void OnOpened(EventArgs e)
    {
        _dialogHost = new SukiDialogHost
        {
            Manager = ((LoginWindowViewModel)DataContext)!.DialogManager
        };

        Hosts.Add(_dialogHost);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_dialogHost != null)
            Hosts.Remove(_dialogHost);

        _dialogHost.Manager = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if(e.Handled) return;
        FocusManager?.ClearFocus();
    }
}