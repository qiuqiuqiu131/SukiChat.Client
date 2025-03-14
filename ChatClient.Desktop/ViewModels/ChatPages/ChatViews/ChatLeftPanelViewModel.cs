using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatLeftPanelViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;

    public ChatViewModel ChatViewModel { get; init; }

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }


    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }
    public DelegateCommand<FriendChatDto> FriendSelectionChangedCommand { get; init; }
    public DelegateCommand<GroupChatDto> GroupSelectionChangedCommand { get; init; }

    public ChatLeftPanelViewModel(ChatViewModel chatViewModel, IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _dialogService = containerProvider.Resolve<IDialogService>();

        ChatViewModel = chatViewModel;

        FriendSelectionChangedCommand = new DelegateCommand<FriendChatDto>(ChatViewModel.FriendSelectionChanged);
        GroupSelectionChangedCommand = new DelegateCommand<GroupChatDto>(ChatViewModel.GroupSelectionChanged);
        CreateGroupCommand = new DelegateCommand(CreateGroup);
        AddNewFriendCommand = new DelegateCommand(AddNewFriend);
    }

    private void AddNewFriend()
    {
        var view = _containerProvider.Resolve<AddNewFriendView>();
        view.Show();
    }

    private async void CreateGroup()
    {
        _dialogService.ShowDialog(nameof(CreateGroupView));
    }
}