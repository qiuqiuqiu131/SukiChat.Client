using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class MainWindowView : SukiWindow, IDisposable
{
    private readonly IUserManager _userManager;
    private TrayIcon? _trayIcon;

    public MainWindowView(IEventAggregator eventAggregator, IUserManager userManager)
    {
        _userManager = userManager;
        InitializeComponent();

        // Icon = new WindowIcon(Environment.CurrentDirectory + "/Assets/DefaultHead.ico");
        eventAggregator.GetEvent<DialogShowEvent>().Subscribe(async show =>
        {
            if (show)
            {
                BackgroundBorder.IsVisible = true;
                BackgroundBorder.Opacity = 1;
            }
            else
            {
                BackgroundBorder.Opacity = 0;
                await Task.Delay(400);
                BackgroundBorder.IsVisible = false;
            }
        });

        eventAggregator.GetEvent<UserMessageBoxShowEvent>().Subscribe(ShowUserMessageBox);
        eventAggregator.GetEvent<GroupMessageBoxShowEvent>().Subscribe(ShowGroupMessageBox);
        eventAggregator.GetEvent<ShowWindowEvent>().Subscribe(ShowWindow);
    }

    #region Message Box

    /// <summary>
    /// 显示群组消息弹窗
    /// </summary>
    /// <param name="obj"></param>
    private void ShowGroupMessageBox(GroupMessageBoxShowEventArgs args)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLevel = GetTopLevel(this);

            // 获取当前鼠标位置和窗口宽度
            var position = args.Args.GetPosition(topLevel);
            var windowWidth = Bounds.Width;
            var windowHeight = Bounds.Height;

            GroupMessageBox.DataContext = args.Group;
            GroupMessageBox.VerticalOffset = 0;
            GroupMessageBox.HorizontalOffset = 0;

            GroupMessageBox.PlacementTarget = args.Args.Source as Control;
            GroupMessageBox.Placement = args.PlacementMode;

            GroupMessageBox.UpdateLayout();

            GroupMessageBox.IsOpen = true;
        });
    }

    /// <summary>
    /// 显示用户消息弹窗
    /// </summary>
    /// <param name="args"></param>
    private void ShowUserMessageBox(UserMessageBoxShowArgs args)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLevel = GetTopLevel(this);

            // 获取当前鼠标位置和窗口宽度
            var position = args.Args.GetPosition(topLevel);
            var windowWidth = Bounds.Width;
            var windowHeight = Bounds.Height;

            //var MessageBox = messageBoxTemplate.Build(args.User) as Popup;

            MessageBox.DataContext = args.User;
            MessageBox.VerticalOffset = 0;
            MessageBox.HorizontalOffset = 0;

            // 如果鼠标在窗口右侧1/3的位置，显示在鼠标的左下角
            if (args.PlacementMode != null)
            {
                MessageBox.PlacementTarget = args.Args.Source as Control;
                MessageBox.Placement = args.PlacementMode.Value;
            }
            else if (position.X > (windowWidth / 2))
            {
                MessageBox.PlacementTarget = args.Args.Source as Control;
                MessageBox.Placement = PlacementMode.LeftEdgeAlignedTop;
            }
            else // 否则显示在鼠标的右下角
            {
                MessageBox.PlacementTarget = args.Args.Source as Control;
                MessageBox.Placement = PlacementMode.RightEdgeAlignedTop;
            }

            if (args.BottomCheck)
            {
                // 如果鼠标在窗口下方1/3处，设置垂直偏移，使弹窗保持在Y轴1/3处
                if (position.Y > (windowHeight * 2 / 3))
                {
                    // 计算需要的偏移量：将弹窗位置调整到窗口高度的1/3处
                    // 这里使用负值因为我们想让弹窗向上移动
                    MessageBox.VerticalOffset = -(position.Y - windowHeight * 2 / 3);
                }
            }

            MessageBox.UpdateLayout();

            MessageBox.IsOpen = true;
        });
    }

    #endregion

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ShowTrayIcon();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        HideTrayIcon();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        Hide();
        _userManager.WindowState = MainWindowState.Close;

        TranslateWindowHelper.CloseAllDialog();

        // 显示trayIcon
        // Close();
    }

    #region Tray Icon

    private void ShowTrayIcon()
    {
        // 避免重复创建
        if (_trayIcon != null)
            return;

        _trayIcon = new TrayIcon
        {
            Icon = Icon,
            ToolTipText = "Suki Chat",
            IsVisible = true,
            Command = new DelegateCommand(ShowWindow),
            Menu = new NativeMenu
            {
                Items =
                {
                    new NativeMenuItem
                    {
                        Header = "显示窗口",
                        Command = new DelegateCommand(ShowWindow)
                    },
                    new NativeMenuItem
                    {
                        Header = "退出",
                        Command = new DelegateCommand(() =>
                        {
                            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                                desktop)
                                desktop.Shutdown();
                        })
                    }
                }
            }
        };
    }

    private void HideTrayIcon()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    #endregion

    private void ShowWindow()
    {
        if (_userManager.WindowState == MainWindowState.Show) return;

        Show();
        WindowState = WindowState.Normal;

        Focus();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Window.WindowStateProperty)
        {
            if (WindowState == WindowState.Minimized)
                _userManager.WindowState = MainWindowState.Hide;
        }
    }

    public override void Show()
    {
        base.Show();
        _userManager.WindowState = MainWindowState.Show;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled) return;
        FocusManager?.ClearFocus();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ShowUserMessageBox(new UserMessageBoxShowArgs(_userManager.User.UserDto!, e) { BottomCheck = false });
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
                HideTrayIcon();
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