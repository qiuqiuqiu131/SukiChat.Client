using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.SideRegion;

public class GroupSideEditViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IUserManager _userManager;
    private readonly IEventAggregator _eventAggregator;
    private IRegionNavigationService _regionManager;

    private GroupRelationDto _selectedGroup;

    public GroupRelationDto SelectedGroup
    {
        get => _selectedGroup;
        set => SetProperty(ref _selectedGroup, value);
    }

    public DelegateCommand ReturnCommand { get; }

    public DelegateCommand EditHeadCommand { get; }

    public GroupSideEditViewModel(ISukiDialogManager sukiDialogManager, IUserManager userManager,
        IEventAggregator eventAggregator)
    {
        _sukiDialogManager = sukiDialogManager;
        _userManager = userManager;
        _eventAggregator = eventAggregator;

        ReturnCommand = new DelegateCommand(Return);
        EditHeadCommand = new DelegateCommand(EditHead);
    }

    // 编辑头像
    private async void EditHead()
    {
        if (SelectedGroup.GroupDto == null) return;

        // 获取文件地址
        string filePath = "";
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            var handle = window!.TryGetPlatformHandle()?.Handle;

            if (handle == null) return;

            var systemFileDialog = App.Current.Container.Resolve<ISystemFileDialog>();
            filePath = await systemFileDialog.OpenFileAsync(handle.Value, "选择图片", "",
                ["Image Files", "*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.webp"]);
        }

        if (string.IsNullOrWhiteSpace(filePath)) return;

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "文件不存在",
                Type = NotificationType.Error
            });
            return;
        }

        Bitmap? HeadImage = null;
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                HeadImage = new Bitmap(stream);
        }
        catch (Exception e)
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "图片格式不正确,无法读取",
                Type = NotificationType.Error
            });
            return;
        }

        // 打开头像编辑窗口
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new EditGroupHeadViewModel(d, new DialogParameters
            {
                { "Image", HeadImage }
            }, async result =>
            {
                if (result.Result != ButtonResult.OK) return;

                var image = result.Parameters.GetValue<Bitmap>("Image");

                var groupService = App.Current.Container.Resolve<IGroupService>();
                var res = await groupService.EditGroupHead(_userManager.User.Id, SelectedGroup.Id, image);

                if (!res.Item1)
                {
                    _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                    {
                        Message = "头像修改失败",
                        Type = NotificationType.Error
                    });
                }
                else
                {
                    _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                    {
                        Message = "头像修改成功",
                        Type = NotificationType.Success
                    });

                    SelectedGroup.GroupDto.HeadImage = image;
                }
            })).TryShow();
    }

    private void Return()
    {
        _regionManager.Journal.GoBack();
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedGroup = navigationContext.Parameters.GetValue<GroupRelationDto>("SelectedGroup");
        _regionManager = navigationContext.NavigationService;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    #endregion

    public bool KeepAlive => false;
}