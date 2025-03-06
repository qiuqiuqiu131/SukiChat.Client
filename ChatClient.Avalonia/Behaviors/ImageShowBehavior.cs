using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactivity;
using ChatClient.Tool.Tools;

namespace ChatClient.Avalonia.Behaviors;

public class ImageShowBehavior : Behavior<Image>
{
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
                OpenImage(image);
                _stopwatch.Reset();
            }
            else
            {
                // 时间间隔过长，重新开始计时
                _stopwatch.Restart();
            }
        }
    }

    private void OpenImage(Image image)
    {
        Bitmap bitmap = (Bitmap)image.Source;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }
}