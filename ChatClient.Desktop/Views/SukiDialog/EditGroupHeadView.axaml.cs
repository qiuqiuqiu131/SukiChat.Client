using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Tools;

namespace ChatClient.Desktop.Views.SukiDialog;

public partial class EditGroupHeadView : UserControl
{
    private bool isLoaded = false;

    public static readonly StyledProperty<Bitmap> ImageProperty = AvaloniaProperty.Register<EditGroupHeadView, Bitmap>(
        "Image");

    public Bitmap Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public EditGroupHeadView()
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

        var dataContext = (EditGroupHeadViewModel)DataContext!;
        dataContext.View = this;

        ImageChanged(Image);
    }

    private double pixcel_width = 0;
    private double pixcel_height = 0;
    private double actual_width = 0;
    private double actual_height = 0;

    private double scale = 1;
    private double moveX = 0;
    private double moveY = 0;

    private double Scale
    {
        get => scale;
        set
        {
            scale = value;
            _scaleTransform.ScaleX = scale;
            _scaleTransform.ScaleY = scale;
        }
    }

    /// <summary>
    /// 图片变换
    /// </summary>
    /// <param name="obj"></param>
    private void ImageChanged(Bitmap obj)
    {
        if (obj == null) return;

        actual_width = obj.PixelSize.Width;
        actual_height = obj.PixelSize.Height;

        if (actual_width > actual_height)
        {
            pixcel_height = 300;
            pixcel_width = actual_width * 300 / actual_height;

            moveX = -(pixcel_width - 300) / 2;
            _translateTransform.X = moveX;
            moveY = 0;
            _translateTransform.Y = moveY;
        }
        else
        {
            pixcel_width = 300;
            pixcel_height = actual_height * 300 / actual_width;

            moveY = -(pixcel_height - 300) / 2;
            _translateTransform.Y = moveY;
            moveX = 0;
            _translateTransform.X = moveX;
        }

        HeadImage.Width = pixcel_width;
        HeadImage.Height = pixcel_height;
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
        double rangeX = pixcel_width * scale - 300;
        double rangeY = pixcel_height * scale - 300;

        if (moveX > 0)
            moveX = 0;
        else if (moveX < -rangeX)
            moveX = -rangeX;
        _translateTransform.X = moveX;

        if (moveY > 0)
            moveY = 0;
        else if (moveY < -rangeY)
            moveY = -rangeY;
        _translateTransform.Y = moveY;
    }

    #region 图片拖动

    private bool isDraged = false;
    private Point previousPoint;

    /// <summary>
    /// HeadImage 按下
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        isDraged = true;
        this.Cursor = new Cursor(StandardCursorType.SizeAll);
        previousPoint = e.GetPosition(HeadImage);
    }

    /// <summary>
    /// HeadImage 释放
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        isDraged = false;
        this.Cursor = new Cursor(StandardCursorType.Arrow);
        previousPoint = new Point();
    }

    /// <summary>
    /// HeadImage 移动
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HeadImage_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (isDraged)
        {
            var currentPoint = e.GetPosition(HeadImage);
            var offsetX = currentPoint.X - previousPoint.X;
            var offsetY = currentPoint.Y - previousPoint.Y;
            previousPoint = currentPoint;

            double rangeX = pixcel_width * scale - 300;
            double rangeY = pixcel_height * scale - 300;

            moveX += offsetX * 0.75;
            if (moveX > 0)
                moveX = 0;
            else if (moveX < -rangeX)
                moveX = -rangeX;
            _translateTransform.X = moveX;

            moveY += offsetY * 0.5;
            if (moveY > 0)
                moveY = 0;
            else if (moveY < -rangeY)
                moveY = -rangeY;
            _translateTransform.Y = moveY;
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
            Bitmap = Image,
            Scale = Scale,
            MoveX = -moveX / (pixcel_width * scale),
            MoveY = -moveY / (pixcel_height * scale)
        };
    }
}