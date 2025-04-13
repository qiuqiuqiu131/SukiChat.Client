using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChatClient.Desktop.ViewModels.CallViewModel;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views;

public partial class SukiCallDialogWindow : SukiWindow, IDialogWindow
{
    public SukiCallDialogWindow()
    {
        InitializeComponent();
    }

    public IDialogResult Result { get; set; }

    public string peerId => ((Content as Control)?.DataContext as ICallView)?.peerId ?? "";
}