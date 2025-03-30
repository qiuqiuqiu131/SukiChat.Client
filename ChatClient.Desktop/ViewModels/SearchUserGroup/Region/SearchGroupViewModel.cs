using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
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

public class SearchGroupViewModel : BindableBase, INavigationAware, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;
    private readonly IUserManager _userManager;

    private INotificationMessageManager? _notificationManager;

    private SubscriptionToken token;

    private Subject<string> searchFriendSubject = new();
    private IDisposable searchFriendDisposable;

    private IEnumerable<GroupDto>? _groupDtos;

    public IEnumerable<GroupDto>? GroupDtos
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

    public DelegateCommand<GroupDto> AddGroupRequestCommand { get; }
    public DelegateCommand<GroupDto> SendMessageCommand { get; }

    public bool IsEmpty => GroupDtos?.Any() != true;

    public SearchGroupViewModel(IContainerProvider containerProvider,
        IDialogService dialogService,
        IEventAggregator eventAggregator,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _dialogService = dialogService;
        _userManager = userManager;

        AddGroupRequestCommand = new DelegateCommand<GroupDto>(AddGroupRequest);
        SendMessageCommand = new DelegateCommand<GroupDto>(SendMessage);

        token = eventAggregator.GetEvent<SearchNewDtoEvent>().Subscribe(OnSearchContentChanged);

        searchFriendDisposable = searchFriendSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(SearchGroup);
    }

    private void OnSearchContentChanged(string searchText) => searchFriendSubject.OnNext(searchText);

    /// <summary>
    /// 导航到聊天页面
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessage(GroupDto obj)
    {
        var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
        var relation = await userDtoManager.GetGroupRelationDto(_userManager.User!.Id, obj.Id);
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
    private void AddGroupRequest(GroupDto obj)
    {
        _dialogService.Show(nameof(AddGroupRequestView), new DialogParameters
        {
            { "GroupDto", obj },
            { "notificationManager", _notificationManager }
        }, e => { });
    }

    private async void SearchGroup(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            GroupDtos = null;
            return;
        }

        var searchService = _containerProvider.Resolve<ISearchService>();
        var result = await searchService.SearchGroupAsync(_userManager.User!.Id, searchText);

        if (result.Count == 0) return;

        GroupDtos = result;
    }

    #region Navigation

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        string searchText = navigationContext.Parameters["searchText"] as string ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
            searchFriendSubject.OnNext(searchText);
        _notificationManager = navigationContext.Parameters["notificationManager"] as INotificationMessageManager;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _notificationManager = null;
    }

    #endregion

    public void Destroy()
    {
        token.Dispose();
        searchFriendDisposable.Dispose();
    }
}