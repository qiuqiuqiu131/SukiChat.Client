using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Notification;
using ChatClient.DataBase.Data;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Data;
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

public class LocalSearchUserViewModel : BindableBase, INavigationAware, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    private INotificationMessageManager? _notificationManager;

    private SubscriptionToken token;

    private Subject<string> searchFriendSubject = new();

    private IDisposable searchFriendDisposable;

    private IEnumerable<FriendSearchDto>? _friendSearchDtos;

    public IEnumerable<FriendSearchDto>? FriendSearchDtos
    {
        get => _friendSearchDtos;
        set => SetProperty(ref _friendSearchDtos, value);
    }

    public bool IsEmpty => FriendSearchDtos?.Any() != true;

    public DelegateCommand<FriendRelationDto> SendMessageCommand { get; }

    public LocalSearchUserViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator)
    {
        _containerProvider = containerProvider;
        _userManager = containerProvider.Resolve<IUserManager>();

        SendMessageCommand = new DelegateCommand<FriendRelationDto>(SendMessage);

        token = eventAggregator.GetEvent<LocalSearchEvent>().Subscribe(OnSearchContentChanged);

        searchFriendDisposable = searchFriendSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchFriend);
    }

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(FriendRelationDto obj)
    {
        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(obj);
        TranslateWindowHelper.ActivateMainWindow();
    }

    private void OnSearchContentChanged(string searchText) => searchFriendSubject.OnNext(searchText);

    private async void SearchFriend(string searchText)
    {
        // TODO:查找好友
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
            searchFriendSubject.OnNext(searchText);
        _notificationManager = navigationContext.Parameters["notificationManager"] as INotificationMessageManager;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _notificationManager = null;
    }

    public void Destroy()
    {
        token.Dispose();
        searchFriendDisposable.Dispose();
    }

    #endregion
}