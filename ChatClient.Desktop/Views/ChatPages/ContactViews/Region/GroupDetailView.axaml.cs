using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;
using ChatClient.Tool.ManagerInterface;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Region;

[RegionMemberLifetime(KeepAlive = true)]
public partial class GroupDetailView : UserControl
{
    public GroupDetailView()
    {
        InitializeComponent();
    }

    private void Head_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var groupRelationDto = ((GroupDetailViewModel)DataContext!).Group;
        var appDataManager = App.Current.Container.Resolve<IAppDataManager>();
        if (groupRelationDto.GroupDto.HeadIndex != -1 && groupRelationDto.GroupDto.IsCustomHead)
        {
            string path = appDataManager.GetFilePath(Path.Combine("Groups", groupRelationDto.GroupDto.Id, "HeadImage",
                $"head_{groupRelationDto.GroupDto.HeadIndex}.png"));
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
        else if (groupRelationDto.GroupDto.IsCustomHead)
        {
            string path = appDataManager.GetFilePath(Path.Combine("Groups", "HeadImage",
                $"{groupRelationDto.GroupDto.HeadIndex}.png"));
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
}