using System.Collections.Generic;
using Avalonia.Collections;
using ChatClient.Desktop.Views.SystemSetting;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Data;
using Material.Icons;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class SystemSettingViewModel : BindableBase, IDialogAware
{
    public IAvaloniaReadOnlyList<SettingBarDto> SettingBars { get; } = new AvaloniaList<SettingBarDto>
    {
        new() { Name = "个性化", Icon = MaterialIconKind.Settings, NavigationTarget = nameof(ThemeView) },
        new() { Name = "账号与安全", Icon = MaterialIconKind.Safe, NavigationTarget = nameof(UndoView) }
    };

    public DelegateCommand CancelCommand { get; }

    public SystemSettingViewModel()
    {
        CancelCommand = new DelegateCommand(() => { RequestClose.Invoke(ButtonResult.Cancel); });
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