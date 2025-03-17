using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class MainWindowView : SukiWindow, IDisposable
{
    private readonly IEventAggregator _eventAggregator;

    public MainWindowView(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();

        // 字体渲染模式：Alias - 锯齿、Antialias - 抗锯齿、SubpixelAntialias - 次像素抗锯齿
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);

        // 如果发现图片呈现不清晰，应该可以通过该配置优化渲染：位图插值模式
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        // 如果发现形状边缘有锯齿，应该可以通过该配置优化渲染：边缘渲染模式
        RenderOptions.SetEdgeMode(this, EdgeMode.Antialias);
    }

    private SukiDialogHost? _dialogHost;
    private SukiToastHost? _toastHost;

    protected override void OnOpened(EventArgs e)
    {
        _dialogHost = new SukiDialogHost
        {
            Manager = ((MainWindowViewModel)DataContext)!.DialogManager
        };

        _toastHost = new SukiToastHost
        {
            Manager = ((MainWindowViewModel)DataContext)!.ToastManager
        };

        Hosts.Add(_dialogHost);
        Hosts.Add(_toastHost);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_dialogHost != null)
            Hosts.Remove(_dialogHost);
        if (_toastHost != null)
            Hosts.Remove(_toastHost);

        _dialogHost.Manager = null;
        _toastHost.Manager = null;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "用户" });
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(Environment.CurrentDirectory + "/Assets/DefaultHead.ico"),
            ToolTipText = "My Application",
            Menu = new NativeMenu
            {
                Items =
                {
                    new NativeMenuItem
                    {
                        Header = "Show",
                        Command = new DelegateCommand(() =>
                        {
                            Show();
                            WindowState = WindowState.Normal;
                        })
                    },
                    new NativeMenuItem
                    {
                        Header = "Exit",
                        Command = new DelegateCommand(() =>
                        {
                            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                                desktop)
                                desktop.Shutdown();
                        })
                    }
                }
            }
        };

        WindowState = WindowState.Minimized;
        Hide();
    }

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
    ~MainWindowView()
    {
        Dispose(false);
    }

    #endregion
}