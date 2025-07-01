using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ChatClient.Android.Shared.Views;

public partial class MainChatView : UserControl
{
    public MainChatView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
            }
        }
    }
}