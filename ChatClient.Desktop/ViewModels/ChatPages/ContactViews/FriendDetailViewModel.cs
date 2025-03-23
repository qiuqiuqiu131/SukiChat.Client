using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class FriendDetailViewModel : ViewModelBase, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;

    private FriendRelationDto? _friend;

    public FriendRelationDto? Friend
    {
        get => _friend;
        set => SetProperty(ref _friend, value);
    }

    public DelegateCommand SendMessageCommand { get; init; }

    public AvaloniaList<string>? GroupNames { get; private set; }

    public FriendDetailViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _userManager = userManager;

        SendMessageCommand = new DelegateCommand(SendMessage);

        GroupNames = new AvaloniaList<string>(_userManager.GroupFriends?.Select(d => d.GroupName).Order());
        if (_userManager.GroupFriends != null)
            _userManager.GroupFriends.CollectionChanged += GroupFriendsOnCollectionChanged;
    }

    private async void SendMessage()
    {
        var friend = Friend;
        _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(friend);
    }

    private void GroupFriendsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems)
            {
                if (item is GroupFriendDto groupFriend)
                {
                    // 如果GroupNames中不包含此分组名称，则按顺序插入
                    if (!GroupNames.Contains(groupFriend.GroupName))
                    {
                        // 找到正确的插入位置
                        int insertIndex = 0;
                        while (insertIndex < GroupNames.Count &&
                               string.Compare(GroupNames[insertIndex], groupFriend.GroupName) < 0)
                        {
                            insertIndex++;
                        }

                        // 在正确位置插入
                        GroupNames.Insert(insertIndex, groupFriend.GroupName);
                    }
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var item in e.OldItems)
            {
                if (item is GroupFriendDto groupFriend)
                {
                    // 检查是否有其他分组使用相同的名称
                    bool nameStillInUse = _userManager.GroupGroups?.Any(g => g.GroupName == groupFriend.GroupName) ??
                                          false;

                    // 如果没有其他分组使用此名称，则从列表中移除
                    if (!nameStillInUse && GroupNames.Contains(groupFriend.GroupName))
                    {
                        GroupNames.Remove(groupFriend.GroupName);
                    }
                }
            }
        }
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Friend = parameters.GetValue<FriendRelationDto>("dto");
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Friend = null;
    }

    public void Destroy()
    {
        if (_userManager.GroupFriends != null)
            _userManager.GroupFriends.CollectionChanged -= GroupFriendsOnCollectionChanged;
    }
}