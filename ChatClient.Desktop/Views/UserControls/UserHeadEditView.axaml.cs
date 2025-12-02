using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Tools;

namespace ChatClient.Desktop.Views.UserControls;

public partial class UserHeadEditView : UserControl
{
    private bool isLoaded = false;

    public UserHeadEditView()
    {
        InitializeComponent();
        SubButton.Click += SubButtonOnClick;
        AddButton.Click += AddButtonOnClick;
    }

    private void AddButtonOnClick(object? sender, RoutedEventArgs e)
    {
        double value = ScaleSlider.Value;
        value += 0.1;
        if (value < 0) value = 0;
        ScaleSlider.Value = value;
    }

    private void SubButtonOnClick(object? sender, RoutedEventArgs e)
    {
        double value = ScaleSlider.Value;
        value -= 0.1;
        if (value > 1) value = 1;
        ScaleSlider.Value = value;
    }

    private TranslateTransform _translateTransform = new() { X = 0, Y = 0 };
    private ScaleTransform _scaleTransform = new() { ScaleX = 1, ScaleY = 1 };

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (isLoaded) return;

        isLoaded = true;

        // 初始化Picture
        var group = new TransformGroup();
        group.Children.Add(_scaleTransform);
        group.Children.Add(_translateTransform);
        HeadImage.RenderTransform = group;
        HeadImage.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);

        var dataContext = (UserHeadEditViewModel)DataContext!;
        dataContext.View = this;
        ImageChanged(dataContext.CurrentHead);
        dataContext.ImageChanged += ImageChanged;

        ScrollViewer.Offset = new Vector(ScrollViewer.Offset.X, 0);
    }

    private Bitmap? _bitmap;

    private double _pixcelWidth = 0;
    private double _pixcelHeight = 0;
    private double _actualWidth = 0;
    private double _actualHeight = 0;

    private double _scale = 1;
    private double _moveX = 0;
    private double _moveY = 0;

    private double Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _scaleTransform.ScaleX = _scale;
            _scaleTransform.ScaleY = _scale;
        }
    }

    /// <summary>
    /// 图片变换
    /// </summary>
    /// <param name="obj"></param>
    private void ImageChanged(Bitmap? obj)
    {
        if (obj == null) return;

        _bitmap = obj;

        _actualWidth = obj.PixelSize.Width;
        _actualHeight = obj.PixelSize.Height;

        if (_actualWidth > _actualHeight)
        {
            _pixcelHeight = 300;
            _pixcelWidth = _actualWidth * 300 / _actualHeight;

            _moveX = -(_pixcelWidth - 300) / 2;
            _translateTransform.X = _moveX;
            _moveY = 0;
            _translateTransform.Y = _moveY;
        }
        else
        {
            _pixcelWidth = 300;
            _pixcelHeight = _actualHeight * 300 / _actualWidth;

            _moveY = -(_pixcelHeight - 300) / 2;
            _translateTransform.Y = _moveY;
            _moveX = 0;
            _translateTransform.X = _moveX;
        }

        HeadImage.Width = _pixcelWidth;
        HeadImage.Height = _pixcelHeight;
        HeadImage.Source = obj;

        Scale = 1;
        ScaleSlider.Value = 0;
    }

    /// <summary>
    /// Slider 图片缩放
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RangeBase_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        Scale = e.NewValue + 1;

        // 图片缩放后，重新计算移动范围
        double rangeX = _pixcelWidth * _scale - 300;
        double rangeY = _pixcelHeight * _scale - 300;

        if (_moveX > 0)
            _moveX = 0;
        else if (_moveX < -rangeX)
            _moveX = -rangeX;
        _translateTransform.X = _moveX;

        if (_moveY > 0)
            _moveY = 0;
        else if (_moveY < -rangeY)
            _moveY = -rangeY;
        _translateTransform.Y = _moveY;
    }

    #region 图片拖动

    private bool _isDraged = false;
    private Point _previousPoint;

    /// <summary>
    /// HeadImage 按下
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDraged = true;
        Cursor = new Cursor(StandardCursorType.SizeAll);
        _previousPoint = e.GetPosition(HeadImage);
    }

    /// <summary>
    /// HeadImage 释放
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDraged = false;
        this.Cursor = new Cursor(StandardCursorType.Arrow);
        _previousPoint = new Point();
    }

    /// <summary>
    /// HeadImage 移动
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraged)
        {
            var currentPoint = e.GetPosition(HeadImage);
            var offsetX = currentPoint.X - _previousPoint.X;
            var offsetY = currentPoint.Y - _previousPoint.Y;
            _previousPoint = currentPoint;

            double rangeX = _pixcelWidth * _scale - 300;
            double rangeY = _pixcelHeight * _scale - 300;

            _moveX += offsetX * 0.75;
            if (_moveX > 0)
                _moveX = 0;
            else if (_moveX < -rangeX)
                _moveX = -rangeX;
            _translateTransform.X = _moveX;

            _moveY += offsetY * 0.5;
            if (_moveY > 0)
                _moveY = 0;
            else if (_moveY < -rangeY)
                _moveY = -rangeY;
            _translateTransform.Y = _moveY;
        }
    }

    #endregion

    /// <summary>
    /// 获取当前图片缩放和移动
    /// </summary>
    /// <returns></returns>
    public ImageResize GetImageResize()
    {
        return new ImageResize
        {
            Bitmap = _bitmap,
            Scale = Scale,
            MoveX = -_moveX / (_pixcelWidth * _scale),
            MoveY = -_moveY / (_pixcelHeight * _scale)
        };
    }
}