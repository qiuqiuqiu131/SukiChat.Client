using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

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

        Dispatcher.UIThread.Post(() => { Root.Classes.Add("Enter"); });

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