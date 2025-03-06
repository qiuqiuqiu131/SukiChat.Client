using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatClient.Desktop.ViewModels.Login;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.Views.Login;

public partial class RegisterWindowView : SukiWindow
{
    public RegisterWindowView()
    {
        InitializeComponent();

        // 字体渲染模式：Alias - 锯齿、Antialias - 抗锯齿、SubpixelAntialias - 次像素抗锯齿
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);

        // 如果发现图片呈现不清晰，应该可以通过该配置优化渲染：位图插值模式
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        // 如果发现形状边缘有锯齿，应该可以通过该配置优化渲染：边缘渲染模式
        RenderOptions.SetEdgeMode(this, EdgeMode.Antialias);
    }

    protected override void OnOpened(EventArgs e)
    {
        ((RegisterWindowViewModel)DataContext!).RegisterSuccessEvent += () =>
        {
            Dispatcher.UIThread.Invoke(() => { Close(); });
        };

        SukiDialogHost dialogHost = new SukiDialogHost
        {
            Manager = ((RegisterWindowViewModel)DataContext!).DialogManager
        };

        Hosts.Add(dialogHost);
    }

    protected override void OnClosed(EventArgs e)
    {
        Hosts.Clear();
    }
}