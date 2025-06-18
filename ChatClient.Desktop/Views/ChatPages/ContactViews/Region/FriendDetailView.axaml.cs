using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using Prism.Ioc;
using Prism.Navigation.Regions;
using Path = System.IO.Path;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Region;

[RegionMemberLifetime(KeepAlive = true)]
public partial class FriendDetailView : UserControl
{
    public FriendDetailView()
    {
        InitializeComponent();
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var friendRelationDto = ((FriendDetailViewModel)DataContext!).Friend;
        var appDataManager = App.Current.Container.Resolve<IAppDataManager>();
        if (friendRelationDto.UserDto.HeadIndex != -1 && friendRelationDto.UserDto.HeadCount != 0)
        {
            string path = appDataManager.GetFilePath(Path.Combine("Users", friendRelationDto.UserDto.Id, "HeadImage",
                $"head_{friendRelationDto.UserDto.HeadIndex}.png"));
            if (System.IO.File.Exists(path))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(path)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    }
                };
                process.Start();
            }
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
    }
}