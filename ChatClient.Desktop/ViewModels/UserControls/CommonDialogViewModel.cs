using System;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class CommonDialogViewModel : BindableBase, IDialogAware
{
    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }
}