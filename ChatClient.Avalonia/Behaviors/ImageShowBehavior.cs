using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactivity;
using ChatClient.Tool.Data;
using ChatClient.Tool.Tools;

namespace ChatClient.Avalonia.Behaviors;

public class ImageShowBehavior : Behavior<Control>
{
    public static readonly StyledProperty<ImageMessDto?> ImageMessProperty =
        AvaloniaProperty.Register<ImageShowBehavior, ImageMessDto?>(
            "ImageMess");

    public ImageMessDto? ImageMess
    {
        get => GetValue(ImageMessProperty);
        set => SetValue(ImageMessProperty, value);
    }


    // 双击时间间隔阈值（单位：毫秒）
    private const int DoubleClickThreshold = 300;

    private Stopwatch _stopwatch = new Stopwatch();

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed += OnPointerPressed;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image) return;

        if (!_stopwatch.IsRunning)
        {
            // 第一次点击，启动计时器
            _stopwatch.Start();
        }
        else
        {
            // 第二次点击，检查时间间隔
            if (_stopwatch.ElapsedMilliseconds <= DoubleClickThreshold)
            {
                // 触发双击事件
                OpenImage();
                _stopwatch.Reset();
            }
            else
            {
                // 时间间隔过长，重新开始计时
                _stopwatch.Restart();
            }
        }
    }

    private void OpenImage()
    {
        if (ImageMess != null)
        {
            if (System.IO.File.Exists(ImageMess.ActualPath))
                Process.Start(new ProcessStartInfo(ImageMess.ActualPath)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                });
            else if (ImageMess.ImageSource != null)
                ImageTool.OpenImageInSystemViewer(ImageMess.ImageSource);
        }
    }
}