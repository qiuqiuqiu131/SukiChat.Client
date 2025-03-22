using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatClient.Desktop.ViewModels;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class MainWindowView : SukiWindow, IDisposable
{
    private readonly IEventAggregator _eventAggregator;

    private readonly IUserManager _userManager;
    // private readonly IDataTemplate messageBoxTemplate;

    public MainWindowView(IEventAggregator eventAggregator, IUserManager userManager)
    {
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        InitializeComponent();

        // messageBoxTemplate = Resources["MessageBox"]! as IDataTemplate;

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

        // #region TrayIcon
        //
        // var trayIcon = new TrayIcon
        // {
        //     Icon = new WindowIcon(Environment.CurrentDirectory + "/Assets/DefaultHead.ico"),
        //     ToolTipText = "Suki Chat",
        //     Menu = new NativeMenu
        //     {
        //         Items =
        //         {
        //             new NativeMenuItem
        //             {
        //                 Header = "Show",
        //                 Command = new DelegateCommand(() =>
        //                 {
        //                     Show();
        //                     WindowState = WindowState.Normal;
        //                 })
        //             },
        //             new NativeMenuItem
        //             {
        //                 Header = "Exit",
        //                 Command = new DelegateCommand(() =>
        //                 {
        //                     if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
        //                         desktop)
        //                         desktop.Shutdown();
        //                 })
        //             }
        //         }
        //     }
        // };
        //
        // #endregion
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
            if (position.X > (windowWidth / 2))
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

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        Hide();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled) return;
        FocusManager?.ClearFocus();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ShowUserMessageBox(new UserMessageBoxShowArgs(_userManager.User!, e) { BottomCheck = false });
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