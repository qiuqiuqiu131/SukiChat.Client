using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Notification;
using ChatClient.Desktop.Views.SystemSetting;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.UIEntity;
using Material.Icons;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class SystemSettingViewModel : BindableBase, IDialogAware
{
    public IAvaloniaReadOnlyList<SettingBarDto> SettingBars { get; } = new AvaloniaList<SettingBarDto>
    {
        new() { Name = "个性化", Icon = MaterialIconKind.Settings, NavigationTarget = nameof(ThemeView) },
        new() { Name = "账号与安全", Icon = MaterialIconKind.Safe, NavigationTarget = nameof(AccountView) }
    };

    public ISukiDialogManager DialogManager { get; set; } = new SukiDialogManager();
    public IRegionManager RegionManager { get; }
    public INotificationMessageManager NotificationMessageManager { get; set; } = new NotificationMessageManager();

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand<SettingBarDto> SelectionChangedCommand { get; }

    public SystemSettingViewModel(IRegionManager regionManager)
    {
        RegionManager = regionManager.CreateRegionManager();

        CancelCommand = new DelegateCommand(() => { RequestClose.Invoke(ButtonResult.Cancel); });
        SelectionChangedCommand = new DelegateCommand<SettingBarDto>(SelectionChanged);
    }

    private void SelectionChanged(SettingBarDto obj)
    {
        if (obj is null) return;

        if (!string.IsNullOrWhiteSpace(obj.NavigationTarget))
        {
            var parameter = new NavigationParameters();
            parameter.Add("DialogManager", DialogManager);
            parameter.Add("NotificationManager", NotificationMessageManager);
            RegionManager.RequestNavigate(RegionNames.SystemSettingRegion, obj.NavigationTarget, parameter);
        }
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}