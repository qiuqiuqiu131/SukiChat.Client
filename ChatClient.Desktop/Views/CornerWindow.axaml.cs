using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.ManagerInterface;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class CornerWindow : Window, ICornerDialogWindow
{
    private ScaleTransform _scaleTransform;
    private TranslateTransform _translateTransform;

    public CornerWindow()
    {
        InitializeComponent();
    }

    private void InitRenderTransform()
    {
        _scaleTransform = new ScaleTransform(1, 1);
        _scaleTransform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Duration = TimeSpan.FromSeconds(0.25),
                Easing = new CubicEaseInOut(),
                Property = ScaleTransform.ScaleXProperty
            },
            new DoubleTransition
            {
                Duration = TimeSpan.FromSeconds(0.25),
                Easing = new CubicEaseInOut(),
                Property = ScaleTransform.ScaleYProperty
            }
        };
        _translateTransform = new TranslateTransform(Width, 0);
        _translateTransform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Duration = TimeSpan.FromSeconds(0.3),
                Easing = new CubicEaseOut(),
                Property = TranslateTransform.XProperty
            },
            new DoubleTransition
            {
                Duration = TimeSpan.FromSeconds(0.3),
                Easing = new CubicEaseOut(),
                Property = TranslateTransform.YProperty
            }
        };
        RenderTransform = new TransformGroup
        {
            Children = new Transforms
            {
                _scaleTransform,
                _translateTransform
            }
        };
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        InitRenderTransform();
        _translateTransform.X = 0;
    }

    public IDialogResult Result { get; set; }

    public async void BeforeClose()
    {
        _scaleTransform.ScaleX = 0.9;
        _scaleTransform.ScaleY = 0.9;
        await Task.Delay(300);
        _translateTransform.X = Width;
        await Task.Delay(300);
        Close();
    }

    public async void BeforeCloseByService()
    {
        _scaleTransform.ScaleX = 0.3;
        _scaleTransform.ScaleY = 0.3;
        await Task.Delay(250);
        Close();
    }
}