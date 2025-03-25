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

public partial class LoginWindowView : SukiWindow, IDisposable
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

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled) return;
        FocusManager?.ClearFocus();
    }

    #region Dispose

    private bool _isDisposed = false;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (DataContext is IDisposable disposable)
                    disposable.Dispose();
            }

            // 释放非托管资源（如果有的话）

            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数，作为安全网
    /// </summary>
    ~LoginWindowView()
    {
        Dispose(false);
    }

    #endregion
}