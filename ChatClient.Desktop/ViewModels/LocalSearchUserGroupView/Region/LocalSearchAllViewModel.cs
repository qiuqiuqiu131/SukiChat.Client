using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Notification;
using ChatClient.Desktop.Tool;
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
    private readonly IUserManager _userManager;

    private INotificationMessageManager? _notificationManager;

    private SubscriptionToken token;

    private Subject<string> searchGroupSubject = new();

    private IDisposable searchGroupDisposable;

    private AllSearchDto? _allSearchDto;

    public AllSearchDto? AllSearchDto
    {
        get => _allSearchDto;
        set => SetProperty(ref _allSearchDto, value);
    }

    public bool IsEmpty => AllSearchDto != null;

    public DelegateCommand<object> SendMessageCommand { get; }

    public LocalSearchAllViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator)
    {
        _containerProvider = containerProvider;
        _userManager = containerProvider.Resolve<IUserManager>();

        SendMessageCommand = new DelegateCommand<object>(SendMessage);

        token = eventAggregator.GetEvent<LocalSearchEvent>().Subscribe(OnSearchContentChanged);

        searchGroupDisposable = searchGroupSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchGroup);
    }

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(object obj)
    {
        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(obj);
        TranslateWindowHelper.ActivateMainWindow();
    }

    private void OnSearchContentChanged(string searchText) => searchGroupSubject.OnNext(searchText);

    private async void SearchGroup(string searchText)
    {
        // TODO:查找好友
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
            searchGroupSubject.OnNext(searchText);
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
        searchGroupDisposable.Dispose();
    }

    #endregion
}