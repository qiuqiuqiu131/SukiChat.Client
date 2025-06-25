using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.ViewModels.CallViewModel;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class SukiCallDialogWindow : SukiWindow, IDialogWindow
{
    public SukiCallDialogWindow()
    {
        InitializeComponent();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public IDialogResult Result { get; set; }

    public string peerId => ((Content as Control)?.DataContext as ICallView)?.peerId ?? "";
}