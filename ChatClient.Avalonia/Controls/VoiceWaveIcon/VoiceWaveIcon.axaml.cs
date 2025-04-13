using System.Collections.ObjectModel;
using System.Timers;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Timer = System.Threading.Timer;

namespace ChatClient.Avalonia.Controls.VoiceWaveIcon;

public class WaveBar : BindableBase
{
    private double _height;

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public double OriginalHeight { get; set; }
}

public partial class VoiceWaveIcon : TemplatedControl
{
    public static readonly StyledProperty<IBrush> ForegroundProperty = AvaloniaProperty.Register<VoiceWaveIcon, IBrush>(
        "Foreground");

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly StyledProperty<bool> IsPlayingProperty = AvaloniaProperty.Register<VoiceWaveIcon, bool>(
        nameof(IsPlaying), false);

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<VoiceWaveIcon, TimeSpan>(
            "Duration");

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public static readonly StyledProperty<double> MaxWaveHeightProperty =
        AvaloniaProperty.Register<VoiceWaveIcon, double>(
            nameof(MaxWaveHeight), 30.0);

    public double MaxWaveHeight
    {
        get => GetValue(MaxWaveHeightProperty);
        set => SetValue(MaxWaveHeightProperty, value);
    }

    public static readonly StyledProperty<int> MaxBarsProperty =
        AvaloniaProperty.Register<VoiceWaveIcon, int>(
            nameof(MaxBars), 15);

    public static readonly StyledProperty<double> MinWaveHeightProperty =
        AvaloniaProperty.Register<VoiceWaveIcon, double>(
            "MinWaveHeight", 3);

    public double MinWaveHeight
    {
        get => GetValue(MinWaveHeightProperty);
        set => SetValue(MinWaveHeightProperty, value);
    }

    public int MaxBars
    {
        get => GetValue(MaxBarsProperty);
        set => SetValue(MaxBarsProperty, value);
    }

    private ItemsControl _waveItemsControl;
    private ObservableCollection<WaveBar> _waveBars;
    private DispatcherTimer _animationTimer;
    private Random _random = new Random();

    static VoiceWaveIcon()
    {
        IsPlayingProperty.Changed.AddClassHandler<VoiceWaveIcon>((x, e) => x.OnIsPlayingChanged(e));
        DurationProperty.Changed.AddClassHandler<VoiceWaveIcon>((x, e) => x.OnDurationChanged(e));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _waveItemsControl = e.NameScope.Find<ItemsControl>("PART_WaveItemsControl");
        _waveBars = new ObservableCollection<WaveBar>();
        _waveItemsControl.ItemsSource = _waveBars;

        InitializeAnimation();
    }

    private void InitializeAnimation()
    {
        GenerateWaveBars();

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _animationTimer.Tick += AnimationTimer_Elapsed;
    }

    private void GenerateWaveBars()
    {
        if (_waveBars == null)
            return;

        _waveBars.Clear();

        // 根据Duration计算条形数量，最大不超过MaxBars
        int barCount = CalculateBarCount();

        // 定义最小高度差异比例 (占可用高度范围的比例)
        double minHeightDifferenceRatio = 0.2;
        double minHeightDifference = (MaxWaveHeight - MinWaveHeight) * minHeightDifferenceRatio;
        double previousHeight = -1;

        for (int i = 0; i < barCount; i++)
        {
            double height;

            if (i == 0)
            {
                // 第一个条形随机生成高度
                height = _random.NextDouble() * (MaxWaveHeight - MinWaveHeight) + MinWaveHeight;
            }
            else
            {
                // 确保与前一条形有足够差距
                int attempts = 0;
                do
                {
                    height = _random.NextDouble() * (MaxWaveHeight - MinWaveHeight) + MinWaveHeight;
                    attempts++;
                    // 最多尝试5次，防止无限循环
                    if (attempts >= 5) break;
                } while (Math.Abs(height - previousHeight) < minHeightDifference);

                // 如果还是没有足够的差距，强制设置为相反高度区域
                if (Math.Abs(height - previousHeight) < minHeightDifference)
                {
                    if (previousHeight > (MaxWaveHeight + MinWaveHeight) / 2)
                    {
                        // 前一个高度偏高，这个就设为低
                        height = MinWaveHeight + (MaxWaveHeight - MinWaveHeight) * 0.3 * _random.NextDouble();
                    }
                    else
                    {
                        // 前一个高度偏低，这个就设为高
                        height = MaxWaveHeight - (MaxWaveHeight - MinWaveHeight) * 0.3 * _random.NextDouble();
                    }
                }
            }

            _waveBars.Add(new WaveBar { Height = height, OriginalHeight = height });
            previousHeight = height;
        }
    }

    private int CalculateBarCount()
    {
        // 将Duration转换为对应的条形数量，这里简单地根据秒数来决定
        double seconds = Duration.TotalSeconds;
        int barCount = Math.Max(5, Math.Min(MaxBars, (int)(seconds * 1.5)));
        return barCount;
    }

    private void OnDurationChanged(AvaloniaPropertyChangedEventArgs e)
    {
        GenerateWaveBars();
    }

    private void OnIsPlayingChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            StartAnimation();
        }
        else
        {
            StopAnimation();
        }
    }

    private void AnimationTimer_Elapsed(object sender, EventArgs e)
    {
        foreach (var bar in _waveBars)
        {
            // 为每个条形随机更新高度，模拟声波效果
            double newHeight = _random.NextDouble() * MaxWaveHeight;
            bar.Height = newHeight;
        }
    }

    public void StartAnimation()
    {
        _animationTimer?.Start();
    }

    public void StopAnimation()
    {
        _animationTimer?.Stop();

        // 恢复原始高度
        foreach (var bar in _waveBars)
        {
            bar.Height = bar.OriginalHeight;
        }
    }
}