using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ChatClient.Avalonia.Controls.CircleImage;

public partial class CircleImage : UserControl
{
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<CircleImage, double>(
        nameof(SizeProperty));

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly StyledProperty<IImage> ImageProperty = AvaloniaProperty.Register<CircleImage, IImage>(
        nameof(Image));

    public IImage Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public CircleImage()
    {
        InitializeComponent();
    }
}