using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Notification;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Data.SearchData;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
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

    private INotificationMessageManager? _notificationManager;
    private IRegionManager? _regionManager;

    private SubscriptionToken token;

    private Subject<string> searchAllSubject = new();

    private IDisposable searchAllDisposable;

    private string? currentSearchText = string.Empty;

    private AllSearchDto? _allSearchDto;

    public AllSearchDto? AllSearchDto
    {
        get => _allSearchDto;
        set => SetProperty(ref _allSearchDto, value);
    }

    public bool IsEmpty => AllSearchDto != null;

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
        INavigationParameters parameters = new NavigationParameters
        {
            { "searchText", currentSearchText ?? string.Empty },
            { "notificationManager", _notificationManager },
            { "regionManager", _regionManager }
        };
        if (obj == "联系人")
            _regionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchUserView), parameters);
        else if (obj == "群聊")
            _regionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchGroupView), parameters);
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
        if (!string.IsNullOrWhiteSpace(searchText))
            searchAllSubject.OnNext(searchText);
        _notificationManager = navigationContext.Parameters["notificationManager"] as INotificationMessageManager;
        _regionManager = navigationContext.Parameters["regionManager"] as IRegionManager;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _notificationManager = null;
    }

    public void Destroy()
    {
        token.Dispose();
        searchAllSubject.Dispose();
    }

    #endregion
}