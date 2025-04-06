using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactivity;
using ChatClient.Desktop.Views.SukiDialog;
using ChatClient.Tool.Tools;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

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

        string path = parameters.GetValue<string>("Path");
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", path);

        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            HeadImage = new Bitmap(stream);

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