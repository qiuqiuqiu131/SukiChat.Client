using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.About;

public class AboutViewModel : BindableBase, IDialogAware
{
    public DelegateCommand CancelCommand { get; }

    private string _version;

    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    public AboutViewModel()
    {
        //var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
        Version = "1.0.0";
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());
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