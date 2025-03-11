using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Tool.Tools;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class GroupDetailView : UserControl
{
    public GroupDetailView()
    {
        InitializeComponent();
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Bitmap bitmap = ((GroupDetailViewModel)DataContext!).Group.GroupDto.HeadImage;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }
}