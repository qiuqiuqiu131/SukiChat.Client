using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class ImageSizeAdjustBehavior : Behavior<Image>
{
    public static readonly StyledProperty<int> MaxWidthProperty =
        AvaloniaProperty.Register<ImageSizeAdjustBehavior, int>(nameof(MaxWidth), defaultValue: 350);

    public int MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    public static readonly StyledProperty<int> MinWidthProperty =
        AvaloniaProperty.Register<ImageSizeAdjustBehavior, int>(nameof(MinWidth), defaultValue: 150);

    public int MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    public static readonly StyledProperty<int> MaxHeightProperty =
        AvaloniaProperty.Register<ImageSizeAdjustBehavior, int>(nameof(MaxHeight), defaultValue: 300);

    public int MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }

    public static readonly StyledProperty<int> MinHeightProperty =
        AvaloniaProperty.Register<ImageSizeAdjustBehavior, int>(nameof(MinHeight), defaultValue: 150);

    public int MinHeight
    {
        get => GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public static readonly StyledProperty<double> ScaleFactorProperty =
        AvaloniaProperty.Register<ImageSizeAdjustBehavior, double>(
            nameof(ScaleFactor), 2.0); // 默认缩放因子为3

    public double ScaleFactor
    {
        get => GetValue(ScaleFactorProperty);
        set => SetValue(ScaleFactorProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.GetObservable(Image.SourceProperty).Subscribe(OnSourceChanged);
    }

    private void OnSourceChanged(IImage? source)
    {
        if (source is Bitmap bitmap)
        {
            if (bitmap.PixelSize == null) return;

            // 使用缩放因子计算初始尺寸
            double width = bitmap.PixelSize.Width / ScaleFactor;
            double height = bitmap.PixelSize.Height / ScaleFactor;

            // 应用最小尺寸限制
            width = Math.Max(width, MinWidth);
            height = Math.Max(height, MinHeight);

            // 应用最大尺寸限制
            width = Math.Min(width, MaxWidth);
            height = Math.Min(height, MaxHeight);

            // 保持宽高比
            double ratio = bitmap.PixelSize.Width / (double)bitmap.PixelSize.Height;
            if (width / height > ratio)
            {
                width = height * ratio;
            }
            else
            {
                height = width / ratio;
            }

            AssociatedObject!.Width = width;
            AssociatedObject!.Height = height;
        }
    }
}