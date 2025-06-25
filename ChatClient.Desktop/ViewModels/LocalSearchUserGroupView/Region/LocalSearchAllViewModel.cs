using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Data.SearchData;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.LocalSearchUserGroupView.Region;

public class LocalSearchAllViewModel : BindableBase, INavigationAware, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly ILocalSearchService _localSearchService;
    private readonly IUserManager _userManager;

    private LocalSearchUserGroupViewModel? _localSearchUserGroupViewModel;

    private SubscriptionToken token;

    private Subject<string> searchAllSubject = new();

    private IDisposable searchAllDisposable;

    private string? currentSearchText = string.Empty;

    private AllSearchDto? _allSearchDto;

    public AllSearchDto? AllSearchDto
    {
        get => _allSearchDto;
        set
        {
            if (SetProperty(ref _allSearchDto, value))
                RaisePropertyChanged(nameof(IsEmpty));
        }
    }

    public bool IsEmpty => AllSearchDto == null || AllSearchDto.FriendSearchDtos.Count == 0
        && AllSearchDto.GroupSearchDtos.Count == 0;

    public DelegateCommand<object> SendMessageCommand { get; }
    public DelegateCommand<string> SearchMoreCommand { get; }
    public DelegateCommand<object> MoveToRelationCommand { get; }

    public LocalSearchAllViewModel(IContainerProvider containerProvider,
        ILocalSearchService localSearchService,
        IEventAggregator eventAggregator)
    {
        _containerProvider = containerProvider;
        _localSearchService = localSearchService;
        _userManager = containerProvider.Resolve<IUserManager>();

        SendMessageCommand = new DelegateCommand<object>(SendMessage);
        SearchMoreCommand = new DelegateCommand<string>(SearchMore);
        MoveToRelationCommand = new DelegateCommand<object>(MoveToRelation);

        token = eventAggregator.GetEvent<LocalSearchEvent>().Subscribe(OnSearchContentChanged);

        searchAllDisposable = searchAllSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchAll);
    }

    private void MoveToRelation(object obj)
    {
        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        if (obj is FriendRelationDto friendRelationDto)
        {
            TranslateWindowHelper.ActivateMainWindow();
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "通讯录" });
            eventAggregator.GetEvent<MoveToRelationEvent>().Publish(friendRelationDto);
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            TranslateWindowHelper.ActivateMainWindow();
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "通讯录" });
            eventAggregator.GetEvent<MoveToRelationEvent>().Publish(groupRelationDto);
        }
    }

    private void SearchMore(string obj)
    {
        if (_localSearchUserGroupViewModel == null) return;

        if (obj == "联系人")
            _localSearchUserGroupViewModel.SearchIndex = 1;
        else if (obj == "群聊")
            _localSearchUserGroupViewModel.SearchIndex = 2;
    }

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(object obj)
    {
        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        TranslateWindowHelper.ActivateMainWindow();
        eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(obj);
    }

    private void OnSearchContentChanged(string searchText)
    {
        currentSearchText = searchText;
        searchAllSubject.OnNext(searchText);
    }

    private async void SearchAll(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            AllSearchDto = null;
        else
            AllSearchDto = await _localSearchService.SearchAllAsync(_userManager.User.Id, searchText);
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        searchAllSubject.OnNext(searchText);

        _localSearchUserGroupViewModel =
            navigationContext.Parameters["localSearchUserGroupViewModel"] as LocalSearchUserGroupViewModel;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _localSearchUserGroupViewModel = null;
    }

    public void Destroy()
    {
        token.Dispose();
        searchAllDisposable.Dispose();
    }

    #endregion
}