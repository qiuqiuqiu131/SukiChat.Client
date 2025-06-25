using Avalonia.Controls;
using Avalonia.Interactivity;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.Login;

public partial class RegisterView : UserControl
{
    private readonly IRegionManager _regionManager;

    public RegisterView()
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