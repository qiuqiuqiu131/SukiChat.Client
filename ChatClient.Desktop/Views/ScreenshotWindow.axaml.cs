using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Tools;
using DryIoc;
using Prism.Commands;
using Prism.Ioc;
using SukiUI;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class ScreenshotWindow : Window
{
    #region Field

    private readonly TaskCompletionSource<Bitmap> _task;

    private double scale;

    private Bitmap _screenBitmap; // 屏幕截图
    private RenderTargetBitmap _renderTarget; // 渲染结果
    private Point _startPoint; // 鼠标起始点
    private Point _endPoint; // 鼠标结束点
    private bool _isDrawing; // 是否正在绘制选择框

    private bool _hasSelection;

    #endregion

    #region Contorl

    private Border _toolbar; // 添加工具栏字段
    private Border _selectionBorder; // 添加选择框字段
    private Border _infoSizePanel; // 添加信息面板字段
    private Border _infoPointPanel; // 添加信息面板字段
    private Border _maskBorder; // 添加遮罩层字段

    #endregion

    public ScreenshotWindow(TaskCompletionSource<Bitmap> task)
    {
        _task = task;

        scale = App.Current.Container.Resolve<ISystemScalingHelper>().GetScalingFactor();

        InitializeComponent();

        CaptureScreen();

        InitializeSelectionBorder();
        InitializeInfoPanel();
        InitializeToolBar();

        AttachEvents();
    }

    #region Initialize

    private void InitializeToolBar()
    {
        _toolbar = this.GetControl<Border>("Toolbar");
        _toolbar.IsHitTestVisible = false;
        var canvas = this.GetControl<Canvas>("MainCanvas");
        canvas.Children.Remove(_toolbar);
        canvas.Children.Add(_toolbar);
    }

    private void InitializeInfoPanel()
    {
        var textBlock_1 = new TextBlock
        {
            Foreground = Brushes.Black,
            FontSize = 12,
            Background = Brushes.Transparent,
            Padding = new Thickness(5)
        };

        _infoPointPanel = new Border
        {
            Name = "InfoPointPanel",
            Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            Child = textBlock_1,
            IsVisible = false,
            CornerRadius = new CornerRadius(3)
        };

        var textBlock_2 = new TextBlock
        {
            Foreground = Brushes.Black,
            FontSize = 12,
            Background = Brushes.Transparent,
            Padding = new Thickness(5)
        };

        _infoSizePanel = new Border
        {
            Name = "InfoSizePanel",
            Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            Child = textBlock_2,
            IsVisible = false,
            CornerRadius = new CornerRadius(3)
        };

        var canvas = this.GetControl<Canvas>("MainCanvas");
        canvas.Children.Add(_infoPointPanel);
        canvas.Children.Add(_infoSizePanel);
    }

    private void CaptureScreen()
    {
        // 使用 PrintScreen 类捕获屏幕
        var printScreen = new PrintScreen();
        var screenImage = printScreen.CaptureScreen();

        // 将 System.Drawing.Image 转换为字节数组
        using (var ms = new MemoryStream())
        {
            screenImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] imageBytes = ms.ToArray();

            // 将字节数组转换为 Avalonia Bitmap
            using (var stream = new MemoryStream(imageBytes))
            {
                _screenBitmap = new Bitmap(stream);
            }
        }

        // 设置窗口背景为屏幕截图
        Background = new ImageBrush(_screenBitmap);
    }

    private void InitializeSelectionBorder()
    {
        // 创建遮罩层
        _maskBorder = new Border
        {
            Name = "MaskBorder",
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), // 半透明黑色
            IsVisible = true
        };

        // 初始化选择框
        _selectionBorder = new Border
        {
            Name = "SelectionBorder",
            Background = null,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 100, 120)),
            BorderThickness = new Thickness(1),
            IsVisible = false
        };

        var canvas = this.GetControl<Canvas>("MainCanvas");
        canvas.Children.Add(_maskBorder); // 先添加遮罩层
        canvas.Children.Add(_selectionBorder); // 再添加选择框
    }

    private void AttachEvents()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
    }

    #endregion

    #region Command

    public void OnConfirm(object? sender, RoutedEventArgs eventArgs)
    {
        _task.SetResult(_renderTarget);
        Close();
    }

    public async void OnSaveAs(object? sender, RoutedEventArgs eventArgs)
    {
        var handler = this.TryGetPlatformHandle()?.Handle;
        if (handler == null) throw new Exception();

        var path = await SystemFileDialog.SaveFileAsync(handler.Value, "屏幕截图.png", "保存截图", ".png\0*.png*\0");

        if (!string.IsNullOrEmpty(path))
        {
            using var stream = System.IO.File.OpenWrite(path);
            _renderTarget.Save(stream);
            _task.SetCanceled();
            Close();
        }
    }

    public void OnCancel(object? sender, RoutedEventArgs eventArgs)
    {
        _task.SetCanceled();
        Close();
    }

    #endregion

    #region OnEvent

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var canvas = this.GetControl<Canvas>("MainCanvas");
        _maskBorder.Width = canvas.Bounds.Width;
        _maskBorder.Height = canvas.Bounds.Height;
        Canvas.SetTop(_maskBorder, 0);
        Canvas.SetLeft(_maskBorder, 0);

        Cursor = new Cursor(StandardCursorType.Cross);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 检查是否是鼠标右键
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _task.SetCanceled();
            Close();
            return;
        }

        if (_hasSelection) return;

        var point = e.GetPosition(this);
        _startPoint = new Point(point.X * scale, point.Y * scale);
        _isDrawing = true;
        _selectionBorder.IsVisible = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing) return;

        // 获取屏幕缩放比例
        var point = e.GetPosition(this);

        // 应用缩放比例
        _endPoint = new Point(
            point.X * scale,
            point.Y * scale
        );

        UpdateSelectionBorder();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        _infoPointPanel.IsVisible = false; // 隐藏信息面板
        _hasSelection = true; // 显示工具栏

        Cursor = new Cursor(StandardCursorType.Arrow);

        double maxX = Math.Max(_endPoint.X, _startPoint.X) / scale;
        double maxY = Math.Max(_endPoint.Y, _startPoint.X) / scale;

        _toolbar.Opacity = 1;
        _toolbar.IsHitTestVisible = true;

        // 设置工具栏在选择框下方中央
        var toolbarX = maxX - _toolbar.Bounds.Width - 3;
        var toolbarY = maxY + 8.0; // 8像素的间距

        // 确保工具栏不会超出窗口边界
        if (toolbarY + _toolbar.Bounds.Height > Bounds.Height)
        {
            toolbarY = Bounds.Height - _toolbar.Bounds.Height - 8;
        }

        Canvas.SetLeft(_toolbar, toolbarX);
        Canvas.SetTop(_toolbar, toolbarY);

        CaptureSelectedArea();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _task.SetCanceled();
            Close();
        }
    }

    #endregion

    private void UpdateSelectionBorder()
    {
        // 计算实际坐标和尺寸（考虑缩放）
        double x = Math.Min(_startPoint.X, _endPoint.X) / scale;
        double y = Math.Min(_startPoint.Y, _endPoint.Y) / scale;
        double width = Math.Abs(_endPoint.X - _startPoint.X) / scale;
        double height = Math.Abs(_endPoint.Y - _startPoint.Y) / scale;

        _selectionBorder.IsVisible = true;

        Canvas.SetLeft(_selectionBorder, x);
        Canvas.SetTop(_selectionBorder, y);
        _selectionBorder.Width = width;
        _selectionBorder.Height = height;

        // 更新遮罩层的裁剪区域
        var geometry = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd
        };

        // 添加整个窗口区域
        geometry.Children.Add(new RectangleGeometry(new Rect(0, 0, Bounds.Width, Bounds.Height)));
        // 添加选择区域（这个区域会被"挖空"）
        geometry.Children.Add(new RectangleGeometry(new Rect(x, y, width, height)));

        _maskBorder.Clip = geometry;

        // 更新信息Point面板
        var textBlock_1 = (TextBlock)_infoPointPanel.Child!;
        textBlock_1.Text = $"坐标: ( {(int)_endPoint.X} , {(int)_endPoint.Y} )";

        // 设置信息面板位置（考虑缩放）
        var panelX_1 = _endPoint.X / scale + 10;
        var panelY_1 = _endPoint.Y / scale + 10;

        // 确保面板不会超出窗口边界
        if (panelX_1 + _infoPointPanel.Bounds.Width > Bounds.Width)
        {
            panelX_1 = Bounds.Width - _infoPointPanel.Bounds.Width - 10;
        }

        if (panelY_1 + _infoPointPanel.Bounds.Height > Bounds.Height)
        {
            panelY_1 = Bounds.Height - _infoPointPanel.Bounds.Height - 10;
        }

        _infoPointPanel.IsVisible = true;
        Canvas.SetLeft(_infoPointPanel, panelX_1);
        Canvas.SetTop(_infoPointPanel, panelY_1);

        // 更新信息Point面板
        var textBlock_2 = (TextBlock)_infoSizePanel.Child!;
        textBlock_2.Text = $"大小: {(int)(width * scale)} × {(int)(height * scale)}";

        // 设置信息面板位置（考虑缩放）
        var panelX_2 = Math.Min(_endPoint.X, _startPoint.X) / scale;
        var panelY_2 = Math.Min(_endPoint.Y, _startPoint.Y) / scale - 10 - _infoSizePanel.Bounds.Height;

        // 确保面板不会超出窗口边界
        if (panelY_2 < 10)
            panelY_2 = 10;

        _infoSizePanel.IsVisible = true;
        Canvas.SetLeft(_infoSizePanel, panelX_2);
        Canvas.SetTop(_infoSizePanel, panelY_2);
    }

    private void CaptureSelectedArea()
    {
        int x = (int)Math.Floor(Math.Min(_startPoint.X, _endPoint.X));
        int y = (int)Math.Floor(Math.Min(_startPoint.Y, _endPoint.Y));
        int width = (int)Math.Floor(Math.Abs(_endPoint.X - _startPoint.X));
        int height = (int)Math.Floor(Math.Abs(_endPoint.Y - _startPoint.Y));

        var croppedBitmap = new CroppedBitmap(_screenBitmap, new PixelRect(x, y, width, height));

        // Render the CroppedBitmap to a new Bitmap and resize it
        var renderTarget = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using (var ctx = renderTarget.CreateDrawingContext(true))
        {
            ctx.DrawImage(croppedBitmap, new Rect(0, 0, width, height), new Rect(0, 0, width, height));
        }

        _renderTarget = renderTarget;
    }
}