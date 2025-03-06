using Prism.Commands;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class CreateGroupViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    public DelegateCommand OKCommand { get; set; }
    public DelegateCommand CancleCommand { get; set; }

    public CreateGroupViewModel(ISukiDialog sukiDialog)
    {
        _sukiDialog = sukiDialog;
        OKCommand = new DelegateCommand(OK);
        CancleCommand = new DelegateCommand(() => _sukiDialog.Dismiss());
    }

    private void OK()
    {
        _sukiDialog.Dismiss();
    }
}