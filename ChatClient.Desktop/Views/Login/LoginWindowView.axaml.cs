using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views.Login;

public partial class LoginWindowView : SukiWindow, IDisposable
{
    public LoginWindowView()
    {
        InitializeComponent();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = false;
        Loaded += (sender, args) => { Opacity = 1; };
    }

    private SukiDialogHost? _dialogHost;


    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_SETICON = 0x80;

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _ = Task.Run(async () =>
        {
            // 平台特定
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Task.Delay(100);

                var handle = TryGetPlatformHandle()?.Handle;
                if (handle != null)
                {
                    SendMessage(handle.Value, WM_SETICON, IntPtr.Zero, IntPtr.Zero);
                    SendMessage(handle.Value, WM_SETICON, new IntPtr(1), IntPtr.Zero);
                }
            }
        });

        content.Opacity = 1;
        await Task.Delay(100);
        SystemDecorations = SystemDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
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