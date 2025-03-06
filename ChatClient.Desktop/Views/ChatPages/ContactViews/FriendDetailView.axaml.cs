using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Tool.Tools;

namespace ChatClient.Desktop.Views.ContactDetailView;

public partial class FriendDetailView : UserControl
{
    public FriendDetailView()
    {
        InitializeComponent();
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Bitmap bitmap = ((FriendDetailViewModel)DataContext!).Friend.UserDto.HeadImage;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }
}