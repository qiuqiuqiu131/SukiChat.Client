using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.DataBase.Data;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatClient.Tool.UIEntity;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Dialog;

public class ChatFriendDialogViewModel : BindableBase, IDialogAware, IChatDialogID
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    public IRegionManager RegionManager { get; private set; }

    private FriendChatDto? friendChatDto { get; set; }

    public DelegateCommand CloseCommand { get; set; }

    public ChatFriendDialogViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        RegionManager = regionManager.CreateRegionManager();

        CloseCommand = new DelegateCommand(Close);
    }

    private void Close()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.None)
        {
            Parameters = { { "FriendChatDto", friendChatDto } }
        });
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        friendChatDto = null;
        RegionManager.Regions[RegionNames.ChatRightRegion].RemoveAll();
        RegionManager = null;
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        friendChatDto = parameters.GetValue<FriendChatDto>("FriendChatDto");
        Dispatcher.UIThread.Post(() =>
        {
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
                new NavigationParameters
                {
                    { "SelectedFriend", friendChatDto }
                });
        });
    }

    public DialogCloseListener RequestClose { get; }

    #endregion

    #region IDialogID

    public string ID => friendChatDto?.UserId ?? "";
    public FileTarget FileTarget => FileTarget.User;

    #endregion
}