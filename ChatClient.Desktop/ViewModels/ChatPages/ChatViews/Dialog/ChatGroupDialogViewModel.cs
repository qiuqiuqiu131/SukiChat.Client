using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data.Group;
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

public class ChatGroupDialogViewModel : BindableBase, IDialogAware, IChatDialogID
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    public IRegionManager RegionManager { get; private set; }

    private GroupChatDto? groupChatDto { get; set; }

    public DelegateCommand CloseCommand { get; set; }

    public ChatGroupDialogViewModel(IRegionManager regionManager, IContainerProvider containerProvider,
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
            Parameters = { { "GroupChatDto", groupChatDto } }
        });
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        groupChatDto = null;
        RegionManager.Regions[RegionNames.ChatRightRegion].RemoveAll();
        RegionManager = null;
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        groupChatDto = parameters.GetValue<GroupChatDto>("GroupChatDto");
        Dispatcher.UIThread.Post(() =>
        {
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
                new NavigationParameters
                {
                    { "SelectedGroup", groupChatDto }
                });
        });
    }

    public DialogCloseListener RequestClose { get; }

    #endregion

    #region IDialogID

    public string ID => groupChatDto?.GroupId ?? "";
    public FileTarget FileTarget => FileTarget.Group;

    #endregion
}