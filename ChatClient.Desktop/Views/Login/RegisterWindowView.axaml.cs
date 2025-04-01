using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.Login;

public partial class RegisterWindowView : UserControl
{
    private readonly IRegionManager _regionManager;

    public RegisterWindowView()
    {
        InitializeComponent();
    }
}