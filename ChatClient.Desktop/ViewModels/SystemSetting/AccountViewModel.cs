using System;
using System.ComponentModel.DataAnnotations;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class AccountViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;
    private UserDetailDto _userDetailDto;

    public UserDetailDto UserDetailDto
    {
        get => _userDetailDto;
        set => SetProperty(ref _userDetailDto, value);
    }

    private string? origionPhoneNumber;
    private string? origionEmailNumber;

    private ISukiDialogManager? sukiDialogManager;
    private INotificationMessageManager? notificationMessageManager;

    public DelegateCommand EditUserDataCommand { get; init; }
    public DelegateCommand EditPasswordCommand { get; init; }

    public AccountViewModel(IUserManager userManager, IContainerProvider containerProvider)
    {
        _userManager = userManager;
        _containerProvider = containerProvider;
        UserDetailDto = userManager.User;

        EditUserDataCommand = new DelegateCommand(EditUserData);
        EditPasswordCommand = new DelegateCommand(EditPassword);
    }

    private void EditPassword()
    {
        if (sukiDialogManager == null) return;
        sukiDialogManager.CreateDialog()
            .WithViewModel(d =>
                new EditPasswordViewModel(d,
                    new DialogParameters
                        { { "NotificationManager", notificationMessageManager } },
                    null))
            .TryShow();
    }

    private void EditUserData()
    {
        if (sukiDialogManager == null) return;
        sukiDialogManager.CreateDialog()
            .WithViewModel(d => new EditUserDataViewModel(d,
                new DialogParameters
                    { { "UserDto", UserDetailDto.UserDto }, { "NotificationManager", notificationMessageManager } }))
            .TryShow();
    }

    // 邮箱变化时调用
    private async void UserDetailDtoOnOnEmailNumberChanged()
    {
        var attribute = new QEmailAddressAttribute();
        if (!attribute.IsValid(_userDetailDto.EmailNumber))
        {
            _userDetailDto.EmailNumberWithoutEvent = origionEmailNumber;
            notificationMessageManager?.ShowMessage("请输入有效的邮箱地址", NotificationType.Error,
                TimeSpan.FromSeconds(2));
            return;
        }

        var passwordService = _containerProvider.Resolve<IPasswordService>();
        var result = await passwordService.UpdateEmailAsync(_userDetailDto.Id, _userDetailDto.Password,
            _userDetailDto.EmailNumber);
        if (!result.Item1)
        {
            _userDetailDto.EmailNumberWithoutEvent = origionEmailNumber;
            notificationMessageManager?.ShowMessage(result.Item2, NotificationType.Error, TimeSpan.FromSeconds(2));
        }
        else
        {
            notificationMessageManager?.ShowMessage("邮箱修改成功", NotificationType.Success,
                TimeSpan.FromSeconds(2));
            origionEmailNumber = _userDetailDto.EmailNumber;
        }
    }

    // 手机号变化时调用
    private async void UserDetailDtoOnOnPhoneNumberChanged()
    {
        var attribute = new QPhoneAttribute();
        if (!attribute.IsValid(_userDetailDto.PhoneNumber))
        {
            _userDetailDto.PhoneNumberWithoutEvent = origionPhoneNumber;
            notificationMessageManager?.ShowMessage("请输入有效的手机号码", NotificationType.Error,
                TimeSpan.FromSeconds(2));
            return;
        }

        var passwordService = _containerProvider.Resolve<IPasswordService>();
        var result = await passwordService.UpdatePhoneNumberAsync(_userDetailDto.Id, _userDetailDto.Password,
            _userDetailDto.PhoneNumber);
        if (!result.Item1)
        {
            _userDetailDto.PhoneNumberWithoutEvent = origionPhoneNumber;
            notificationMessageManager?.ShowMessage(result.Item2, NotificationType.Error, TimeSpan.FromSeconds(2));
        }
        else
        {
            notificationMessageManager?.ShowMessage("手机号修改成功", NotificationType.Success,
                TimeSpan.FromSeconds(2));
            origionPhoneNumber = _userDetailDto.PhoneNumber;
        }
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        sukiDialogManager = navigationContext.Parameters.GetValue<ISukiDialogManager>("DialogManager");
        notificationMessageManager =
            navigationContext.Parameters.GetValue<INotificationMessageManager>("NotificationManager");

        UserDetailDto.OnPhoneNumberChanged += UserDetailDtoOnOnPhoneNumberChanged;
        UserDetailDto.OnEmailNumberChanged += UserDetailDtoOnOnEmailNumberChanged;

        origionEmailNumber = UserDetailDto.EmailNumber;
        origionPhoneNumber = UserDetailDto.PhoneNumber;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        UserDetailDto.OnEmailNumberChanged -= UserDetailDtoOnOnEmailNumberChanged;
        UserDetailDto.OnPhoneNumberChanged -= UserDetailDtoOnOnPhoneNumberChanged;
    }

    #endregion

    public bool KeepAlive => false;
}