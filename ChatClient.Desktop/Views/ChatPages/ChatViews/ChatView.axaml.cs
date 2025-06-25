using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatView : UserControl
{
    private bool isHide = false;
    private bool isLeftMovable = false;

    public ChatView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (e.NewSize.Width < 450 && !isHide)
        {
            isHide = true;

            Root.ColumnDefinitions[0].MaxWidth = 500;
            Root.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].MinWidth = 0;
            ContentControl.Opacity = 0;

            return;
        }
        else if (e.NewSize.Width >= 460 && isHide)
        {
            isHide = false;

            Root.ColumnDefinitions[0].MaxWidth = 270;
            Root.ColumnDefinitions[1].Width = new GridLength(1.2, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[2].MinWidth = 300;
            ContentControl.Opacity = 1;

            return;
        }

        if (ContentControl.Bounds.Width <= 302 && !isLeftMovable)
        {
            isLeftMovable = true;

            Root.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[0].MaxWidth = 270;
        }
        else if (ContentControl.Bounds.Width > 310 && isLeftMovable)
        {
            isLeftMovable = false;

            Root.ColumnDefinitions[0].Width = new GridLength(270, GridUnitType.Pixel);
            Root.ColumnDefinitions[0].MaxWidth = 330;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var width = Bounds.Width;
        if (width < 100) return;

        if (ContentControl.Bounds.Width <= 302)
        {
            isLeftMovable = true;

            Root.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[0].MaxWidth = 270;
        }

        if (ContentControl.Bounds.Width > 310)
        {
            isLeftMovable = false;

            Root.ColumnDefinitions[0].Width = new GridLength(270, GridUnitType.Pixel);
            Root.ColumnDefinitions[0].MaxWidth = 330;
        }

        if (width < 450)
        {
            isHide = true;

            Root.ColumnDefinitions[0].MaxWidth = 500;
            Root.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].MinWidth = 0;
            ContentControl.Opacity = 0;
        }
        else if (width >= 460)
        {
            isHide = false;

            Root.ColumnDefinitions[0].MaxWidth = 270;
            Root.ColumnDefinitions[1].Width = new GridLength(1.2, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[2].MinWidth = 300;
            ContentControl.Opacity = 1;
        }
    }
}