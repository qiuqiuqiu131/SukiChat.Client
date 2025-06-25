using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Notification;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.SearchUserGroup.Region;

public class SearchAllViewModel : BindableBase, INavigationAware, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;
    private readonly IUserManager _userManager;

    private INotificationMessageManager? _notificationManager;
    private SearchUserGroupViewModel? _searchUserGroupViewModel;

    private SubscriptionToken token;

    private Subject<string> searchFriendSubject = new();
    private IDisposable searchFriendDisposable;

    private string? currentSearchText = string.Empty;

    private List<UserDto>? _userDtos;

    public List<UserDto>? UserDtos
    {
        get => _userDtos;
        set
        {
            if (SetProperty(ref _userDtos, value))
            {
                RaisePropertyChanged(nameof(IsEmpty));
            }
        }
    }

    private List<GroupDto>? _groupDtos;

    public List<GroupDto>? GroupDtos
    {
        get => _groupDtos;
        set
        {
            if (SetProperty(ref _groupDtos, value))
            {
                RaisePropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty => !(_userDtos?.Any() ?? false) && !(_groupDtos?.Any() ?? false);

    public DelegateCommand<UserDto> AddFriendRequestCommand { get; }
    public DelegateCommand<GroupDto> AddGroupRequestCommand { get; }
    public DelegateCommand<object> SendMessageCommand { get; }

    public DelegateCommand<string> SearchMoreCommand { get; }

    public SearchAllViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _dialogService = dialogService;
        _userManager = userManager;

        AddFriendRequestCommand = new DelegateCommand<UserDto>(AddFriendRequest);
        AddGroupRequestCommand = new DelegateCommand<GroupDto>(AddGroupRequest);
        SendMessageCommand = new DelegateCommand<object>(SendMessage);
        SearchMoreCommand = new DelegateCommand<string>(SearchMore);

        token = eventAggregator.GetEvent<SearchNewDtoEvent>().Subscribe(OnSearchContentChanged);

        searchFriendDisposable = searchFriendSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchAll);
    }

    private void SearchMore(string obj)
    {
        if (_searchUserGroupViewModel == null) return;

        if (obj == "联系人")
            _searchUserGroupViewModel.SelectedIndex = 1;
        else if (obj == "群聊")
            _searchUserGroupViewModel.SelectedIndex = 2;
    }

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(object obj)
    {
        var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
        object relation;
        if (obj is UserDto user)
        {
            relation = await userDtoManager.GetFriendRelationDto(_userManager.User!.Id, user.Id);
            if (relation == null) return;
        }
        else if (obj is GroupDto group)
        {
            relation = await userDtoManager.GetGroupRelationDto(_userManager.User!.Id, group.Id);
            if (relation == null) return;
        }
        else
            return;

        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(relation);
        TranslateWindowHelper.ActivateMainWindow();
    }

    /// <summary>
    /// 添加好友请求View
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void AddFriendRequest(UserDto obj)
    {
        _dialogService.Show(nameof(AddFriendRequestView), new DialogParameters
        {
            { "UserDto", obj },
            { "notificationManager", _notificationManager }
        }, e => { });
    }

    /// <summary>
    /// 添加群聊请求View
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void AddGroupRequest(GroupDto obj)
    {
        _dialogService.Show(nameof(AddGroupRequestView), new DialogParameters
        {
            { "GroupDto", obj },
            { "notificationManager", _notificationManager }
        }, e => { });
    }

    private void OnSearchContentChanged(string searchText)
    {
        currentSearchText = searchText;
        searchFriendSubject.OnNext(searchText);
    }

    private async void SearchAll(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            searchText = "机器人";

        var searchService = _containerProvider.Resolve<ISearchService>();
        var result = await searchService.SearchAllAsync(_userManager.User!.Id, searchText);

        UserDtos = result.Item1;
        GroupDtos = result.Item2;
    }

    #region Navigation

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        searchFriendSubject.OnNext(searchText);

        _notificationManager = navigationContext.Parameters["notificationManager"] as INotificationMessageManager;
        _searchUserGroupViewModel =
            navigationContext.Parameters["searchUserGroupViewModel"] as SearchUserGroupViewModel;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _notificationManager = null;
        _searchUserGroupViewModel = null;
    }

    #endregion

    public void Destroy()
    {
        token.Dispose();
        searchFriendDisposable.Dispose();
    }
}