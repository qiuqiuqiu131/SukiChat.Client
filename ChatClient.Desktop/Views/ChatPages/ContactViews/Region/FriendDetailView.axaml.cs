using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;
using ChatClient.Tool.Tools;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Region;

[RegionMemberLifetime(KeepAlive = true)]
public partial class FriendDetailView : UserControl
{
    public FriendDetailView()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Bitmap bitmap = ((FriendDetailViewModel)DataContext!).Friend.UserDto.HeadImage;
        ImageTool.OpenImageInSystemViewer(bitmap);
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
    }
}