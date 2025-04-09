using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors;

public class VoiceDurationSizeBehavior : Behavior<Control>
{
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<VoiceDurationSizeBehavior, TimeSpan>(nameof(Duration));

    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<VoiceDurationSizeBehavior, double>(nameof(MinWidth), 120.0);

    public static readonly StyledProperty<double> MaxWidthProperty =
        AvaloniaProperty.Register<VoiceDurationSizeBehavior, double>(nameof(MaxWidth), 280.0);

    /// <summary>
    /// 语音消息的时长
    /// </summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>
    /// 最小宽度
    /// </summary>
    public double MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>
    /// 最大宽度
    /// </summary>
    public double MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DurationProperty ||
            change.Property == MinWidthProperty ||
            change.Property == MaxWidthProperty)
        {
            UpdateWidth();
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        UpdateWidth();
    }

    private void UpdateWidth()
    {
        if (AssociatedObject == null)
            return;

        // 计算基于时长的宽度，按照每秒语音约20像素的宽度
        double baseWidth = Duration.TotalSeconds * 10; // 100是基础宽度(按钮和边距等)

        // 限制在最大和最小宽度范围内
        double width = Math.Max(MinWidth, Math.Min(MaxWidth, baseWidth));

        AssociatedObject.Width = width;
    }
}