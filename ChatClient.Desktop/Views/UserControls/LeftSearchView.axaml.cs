using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.UserControls;

public partial class LeftSearchView : UserControl
{
    public static readonly StyledProperty<ICommand> SearchMoreProperty =
        AvaloniaProperty.Register<LeftSearchView, ICommand>(
            "SearchMore");

    public ICommand SearchMore
    {
        get => GetValue(SearchMoreProperty);
        set => SetValue(SearchMoreProperty, value);
    }

    public LeftSearchView()
    {
        InitializeComponent();
    }
}