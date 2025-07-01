using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace ChatClient.Avalonia.Controls.AudioLevelVisualizer
{
    public partial class AudioLevelVisualizer : UserControl
    {
        private float _currentLevel;
        private DispatcherTimer _updateTimer;
        private Canvas _visualizerCanvas;
        private List<Rectangle> _rectangles = new List<Rectangle>();

        public static readonly StyledProperty<IBrush> IndicatorColorProperty =
            AvaloniaProperty.Register<AudioLevelVisualizer, IBrush>(
                nameof(IndicatorColor),
                new SolidColorBrush(Color.FromRgb(29, 185, 84))); // Spotify绿色

        public IBrush IndicatorColor
        {
            get => GetValue(IndicatorColorProperty);
            set => SetValue(IndicatorColorProperty, value);
        }

        public static readonly StyledProperty<int> BarCountProperty =
            AvaloniaProperty.Register<AudioLevelVisualizer, int>(
                "BarCount", 11);

        public int BarCount
        {
            get => GetValue(BarCountProperty);
            set => SetValue(BarCountProperty, value);
        }

        public static readonly StyledProperty<IEasing> EasingProperty =
            AvaloniaProperty.Register<AudioLevelVisualizer, IEasing>(
                "Easing", new SineEaseOut());

        public IEasing Easing
        {
            get => GetValue(EasingProperty);
            set => SetValue(EasingProperty, value);
        }

        public AudioLevelVisualizer()
        {
            InitializeComponent();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _visualizerCanvas = e.NameScope.Get<Canvas>("VisualizerCanvas");

            // 创建所有矩形并添加到Canvas
            InitializeRectangles();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _updateTimer.Tick += UpdateVisualization;
            _updateTimer.Start();
        }

        private void InitializeRectangles()
        {
            if (_visualizerCanvas == null) return;

            _rectangles.Clear();
            _visualizerCanvas.Children.Clear();

            for (int i = 0; i < BarCount; i++)
            {
                var rect = new Rectangle
                {
                    Fill = IndicatorColor,
                    Height = 0,
                    Width = 0 // 初始宽度设为0，更新时计算
                };

                _rectangles.Add(rect);
                _visualizerCanvas.Children.Add(rect);
            }
        }

        private void UpdateVisualization(object sender, EventArgs e)
        {
            // 更新波形显示
            UpdateWaveform();
        }

        private void UpdateWaveform()
        {
            if (_visualizerCanvas == null || _rectangles.Count == 0)
                return;

            var width = _visualizerCanvas.Bounds.Width;
            var height = _visualizerCanvas.Bounds.Height;

            if (width <= 0 || height <= 0)
                return;

            var barWidth = width / BarCount;
            int middleIndex = BarCount / 2;

            // 更新现有矩形的属性
            for (int i = 0; i < BarCount; i++)
            {
                // 计算当前柱体与中心的距离比例
                double distanceRatio = Math.Abs(i - middleIndex) / (double)middleIndex;

                // 使用缓动函数计算衰减
                double attenuationFactor = 1 - Easing.Ease(distanceRatio);

                // 应用衰减到当前音量
                float level = (float)(_currentLevel * attenuationFactor);
                var barHeight = height * level;

                var rect = _rectangles[i];
                rect.Width = barWidth;
                rect.Height = barHeight;
                rect.Fill = IndicatorColor;

                Canvas.SetLeft(rect, i * barWidth);
                Canvas.SetBottom(rect, 0);
            }
        }

        /// <summary>
        /// 更新音频音量数据
        /// </summary>
        /// <param name="level">当前音量级别，范围0.0-1.0</param>
        public void UpdateLevel(float level)
        {
            // 限制范围
            level = Math.Clamp(level, 0f, 1f);
            _currentLevel = level;
        }

        /// <summary>
        /// 清空音频音量数据
        /// </summary>
        public void Clear()
        {
            _currentLevel = 0;
            foreach (var rect in _rectangles)
                rect.Height = 0;
        }
    }
}