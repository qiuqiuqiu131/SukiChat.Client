using System;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.Views.SukiDialog;
using ChatClient.Tool.Tools;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SukiDialogs;

public class EditGroupHeadViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    private readonly Action<IDialogResult>? _requestClose;

    public Bitmap? HeadImage { get; }

    public EditGroupHeadView? View { get; set; }

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public EditGroupHeadViewModel(ISukiDialog sukiDialog, IDialogParameters parameters,
        Action<IDialogResult>? requestClose)
    {
        _sukiDialog = sukiDialog;
        _requestClose = requestClose;

        HeadImage = parameters.GetValue<Bitmap>("Image");

        CancelCommand = new DelegateCommand(() =>
        {
            requestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            _sukiDialog.Dismiss();
        });
        SaveCommand = new DelegateCommand(Save);
    }

    private void Save()
    {
        if (View == null) return;

        var imageResize = View.GetImageResize();
        Bitmap bitmap = ImageTool.GetHeadImage(imageResize);

        _requestClose?.Invoke(new DialogResult(ButtonResult.OK)
        {
            Parameters = { { "Image", bitmap } }
        });

        _sukiDialog.Dismiss();
    }
}