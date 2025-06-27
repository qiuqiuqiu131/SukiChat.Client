using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatClient.Desktop.Views.ForgetPassword;

public partial class ForgetPasswordView : UserControl
{
    public ForgetPasswordView()
    {
        InitializeComponent();
        Opacity = 0;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Opacity = 1;
    }
}