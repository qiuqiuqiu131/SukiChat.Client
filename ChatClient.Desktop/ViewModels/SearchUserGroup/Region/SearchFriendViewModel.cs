using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.SearchUserGroupView;
using ChatClient.Tool.Data;
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

public class SearchFriendViewModel : BindableBase, INavigationAware, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;
    private readonly IUserManager _userManager;

    private SubscriptionToken token;

    private Subject<string> searchFriendSubject = new();
    private IDisposable searchFriendDisposable;

    private IEnumerable<UserDto>? _userDtos;

    public IEnumerable<UserDto>? UserDtos
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

    public bool IsEmpty => UserDtos?.Any() != true;

    public DelegateCommand<UserDto> AddFriendRequestCommand { get; }
    public DelegateCommand<UserDto> SendMessageCommand { get; }

    public SearchFriendViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _dialogService = dialogService;
        _userManager = userManager;

        AddFriendRequestCommand = new DelegateCommand<UserDto>(AddFriendRequest);
        SendMessageCommand = new DelegateCommand<UserDto>(SendMessage);

        token = eventAggregator.GetEvent<SearchNewDtoEvent>().Subscribe(OnSearchContentChanged);

        searchFriendDisposable = searchFriendSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchFriend);
    }

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(UserDto obj)
    {
        var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
        var relation = await userDtoManager.GetFriendRelationDto(_userManager.User!.Id, obj.Id);
        if (relation == null) return;

        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
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
            { "UserDto", obj }
        }, e => { });
    }

    private void OnSearchContentChanged(string searchText) => searchFriendSubject.OnNext(searchText);

    private async void SearchFriend(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            UserDtos = null;
            return;
        }

        var searchService = _containerProvider.Resolve<ISearchService>();
        var result = await searchService.SearchUserAsync(_userManager.User!.Id, searchText);

        if (result.Count == 0) return;

        UserDtos = result;
    }

    #region Navigation

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
            searchFriendSubject.OnNext(searchText);
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    #endregion

    public void Destroy()
    {
        token.Dispose();
        searchFriendDisposable.Dispose();
    }
}