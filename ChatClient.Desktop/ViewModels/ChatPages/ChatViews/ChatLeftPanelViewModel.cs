using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;
using SearchUserGroupView = ChatClient.Desktop.Views.SearchUserGroupView.SearchUserGroupView;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatLeftPanelViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;
    private readonly ISukiDialogManager _sukiDialogManager;

    public ChatViewModel ChatViewModel { get; init; }

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }


    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }
    public AsyncDelegateCommand<FriendChatDto> FriendSelectionChangedCommand { get; init; }
    public AsyncDelegateCommand<GroupChatDto> GroupSelectionChangedCommand { get; init; }

    public ChatLeftPanelViewModel(ChatViewModel chatViewModel, IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _dialogService = containerProvider.Resolve<IDialogService>();
        _sukiDialogManager = containerProvider.Resolve<ISukiDialogManager>();

        ChatViewModel = chatViewModel;

        FriendSelectionChangedCommand = new AsyncDelegateCommand<FriendChatDto>(ChatViewModel.FriendSelectionChanged);
        GroupSelectionChangedCommand = new AsyncDelegateCommand<GroupChatDto>(ChatViewModel.GroupSelectionChanged);
        CreateGroupCommand = new DelegateCommand(CreateGroup);
        AddNewFriendCommand = new DelegateCommand(AddNewFriend);
    }

    private void AddNewFriend()
    {
        _dialogService.Show(nameof(SearchUserGroupView));
    }

    private void CreateGroup()
    {
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CreateGroupViewModel(d, null))
            .TryShow();
    }
}