using Avalonia.Notification;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views.SukiDialog;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class AccountViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IUserManager _userManager;
    private UserDetailDto _userDetailDto;

    public UserDetailDto UserDetailDto
    {
        get => _userDetailDto;
        set => SetProperty(ref _userDetailDto, value);
    }

    private ISukiDialogManager? sukiDialogManager;
    private INotificationMessageManager? notificationMessageManager;

    public DelegateCommand EditUserDataCommand { get; init; }
    public DelegateCommand EditPasswordCommand { get; init; }

    public AccountViewModel(IUserManager userManager)
    {
        _userManager = userManager;
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

    private void UserDetailDtoOnOnUserDataChanged()
    {
        _userManager.SaveUser();
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        sukiDialogManager = navigationContext.Parameters.GetValue<ISukiDialogManager>("DialogManager");
        notificationMessageManager =
            navigationContext.Parameters.GetValue<INotificationMessageManager>("NotificationManager");

        UserDetailDto.OnUserDataChanged += UserDetailDtoOnOnUserDataChanged;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        UserDetailDto.OnUserDataChanged -= UserDetailDtoOnOnUserDataChanged;
    }

    #endregion

    public bool KeepAlive => false;
}