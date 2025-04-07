using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ChatClient.Desktop.Views;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.Desktop.Tool;

public static class ChatDialogHelper
{
    /// <summary>
    /// 当好友选中状态发生变化时调用
    /// 如果此好友对象处于弹窗状态，那么打开弹窗
    /// 不然，在MainWindow中打开聊天页面
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>如果存在聊天窗口，返回true；否认，返回false</returns>
    public static bool FriendChatSelected(string userId)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var chatDialog = desktop.Windows
                .FirstOrDefault(d =>
                    d is SukiChatDialogWindow { ChatDialogId.FileTarget: FileTarget.User, } dialogWindow &&
                    dialogWindow.ChatDialogId.ID.Equals(userId));
            if (chatDialog != null)
            {
                chatDialog.Activate();
                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// 当群聊选中状态发生变化时调用
    /// 如果此群聊对象处于弹窗状态，那么打开弹窗
    /// 不然，在MainWindow中打开聊天页面
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>如果存在聊天窗口，返回true；否认，返回false</returns>
    public static bool GroupChatSelected(string groupId)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var chatDialog = desktop.Windows
                .FirstOrDefault(d =>
                    d is SukiChatDialogWindow { ChatDialogId.FileTarget: FileTarget.Group, } dialogWindow &&
                    dialogWindow.ChatDialogId.ID.Equals(groupId));
            if (chatDialog != null)
            {
                chatDialog.Activate();
                return true;
            }

            return false;
        }

        return false;
    }
}