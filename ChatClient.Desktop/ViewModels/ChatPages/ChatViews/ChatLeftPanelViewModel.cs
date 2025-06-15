using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.SearchService;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Data.SearchData;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatLeftPanelViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly ILocalSearchService _localSearchService;
    private readonly IDialogService _dialogService;
    private readonly ISukiDialogManager _sukiDialogManager;

    public ChatViewModel ChatViewModel { get; init; }

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                searchSubject.OnNext(value);
        }
    }

    private AllSearchDto? _allSearchDto;

    public AllSearchDto? AllSearchDto
    {
        get => _allSearchDto;
        set => SetProperty(ref _allSearchDto, value);
    }

    private Subject<string> searchSubject = new();
    private IDisposable searchDisposable;


    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }
    public DelegateCommand<string> SearchMoreCommand { get; init; }
    public AsyncDelegateCommand<FriendChatDto> FriendSelectionChangedCommand { get; init; }
    public AsyncDelegateCommand<GroupChatDto> GroupSelectionChangedCommand { get; init; }
    public AsyncDelegateCommand<FriendChatDto> FriendOpenDialogCommand { get; init; }
    public AsyncDelegateCommand<GroupChatDto> GroupOpenDialogCommand { get; init; }

    public ChatLeftPanelViewModel(ChatViewModel chatViewModel, IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _userManager = containerProvider.Resolve<IUserManager>();
        _localSearchService = containerProvider.Resolve<ILocalSearchService>();
        _dialogService = containerProvider.Resolve<IDialogService>();
        _sukiDialogManager = containerProvider.Resolve<ISukiDialogManager>();

        ChatViewModel = chatViewModel;

        FriendSelectionChangedCommand = new AsyncDelegateCommand<FriendChatDto>(ChatViewModel.FriendSelectionChanged);
        GroupSelectionChangedCommand = new AsyncDelegateCommand<GroupChatDto>(ChatViewModel.GroupSelectionChanged);
        CreateGroupCommand = new DelegateCommand(CreateGroup);
        AddNewFriendCommand = new DelegateCommand(AddNewFriend);
        SearchMoreCommand = new DelegateCommand<string>(SearchMore);
        FriendOpenDialogCommand = new AsyncDelegateCommand<FriendChatDto>(ChatViewModel.FriendOpenDialog);
        GroupOpenDialogCommand = new AsyncDelegateCommand<GroupChatDto>(ChatViewModel.GroupOpenDialog);

        searchDisposable = searchSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchAll);
    }

    private void SearchMore(string obj)
    {
        _dialogService.Show(nameof(LocalSearchUserGroupView),
            new DialogParameters { { "SearchText", _searchText }, { "SearchType", obj } }, null);
        SearchText = null;
    }

    private async void SearchAll(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AllSearchDto = null;
        }
        else
        {
            AllSearchDto = await _localSearchService.SearchAllAsync(_userManager.User.Id, searchText);
        }
    }

    private async void AddNewFriend()
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