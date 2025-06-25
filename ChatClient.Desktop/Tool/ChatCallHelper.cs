using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChatClient.Desktop.Views;
using Prism.Dialogs;

namespace ChatClient.Desktop.Tool;

public static class ChatCallHelper
{
    public static bool OpenCallDialog(string peerId)
    {
        var result = Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var callDialog = desktop.Windows
                    .FirstOrDefault(d =>
                        d is SukiCallDialogWindow callWindow && callWindow.peerId.Equals(peerId));
                if (callDialog != null)
                {
                    callDialog.Activate();
                    return true;
                }

                return false;
            }

            return false;
        });

        return result;
    }

    public static void CloseOtherCallDialog()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var callDialog = desktop.Windows
                    .Where(d =>
                        d is SukiCallDialogWindow).ToList();

                foreach (var dialog in callDialog)
                {
                    if (dialog.Content is Control { DataContext: IDialogAware dialogAware })
                        dialogAware.RequestClose.Invoke();
                    else
                        dialog.Close();
                }
            }
        });
    }
}