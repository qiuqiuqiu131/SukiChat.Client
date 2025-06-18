using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views.ChatPages.ChatViews.SideRegion;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
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
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.SideRegion;

public class GroupSideViewModel : BindableBase, INavigationAware
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IUserManager _userManager;

    private IRegionNavigationService _regionManager;

    private GroupRelationDto? _selectedGroup;

    public GroupRelationDto? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
                RaisePropertyChanged(nameof(GroupMembers));
        }
    }

    public List<object> GroupMembers
    {
        get
        {
            if (SelectedGroup == null || SelectedGroup.GroupDto == null)
                return [];

            var list = new List<object>();
            if (SelectedGroup.IsOwner)
            {
                var member = SelectedGroup.GroupDto!.GroupMembers.OrderBy(d => d.Status)
                    .ThenBy(d => d.JoinTime).Take(13);
                list.AddRange(member);
                list.Add("Minus");
                list.Add("Add");
            }
            else
            {
                var member = SelectedGroup.GroupDto!.GroupMembers.OrderBy(d => d.Status)
                    .ThenBy(d => d.JoinTime).Take(14);
                list.AddRange(member);
                list.Add("Add");
            }

            return list;
        }
    }

    public DelegateCommand DeleteGroupCommand { get; }
    public DelegateCommand QuitGroupCommand { get; }
    public DelegateCommand EditGroupCommand { get; }
    public DelegateCommand GroupMemberCommand { get; }
    public DelegateCommand InviteMemberCommand { get; }
    public DelegateCommand RemoveMemberCommand { get; }
    public DelegateCommand CanDisturbCommand { get; }

    public GroupSideViewModel(IEventAggregator eventAggregator, IContainerProvider containerProvider,
        ISukiDialogManager sukiDialogManager,
        IUserManager userManager)
    {
        _eventAggregator = eventAggregator;
        _containerProvider = containerProvider;
        _sukiDialogManager = sukiDialogManager;
        _userManager = userManager;

        DeleteGroupCommand = new DelegateCommand(DeleteGroup);
        QuitGroupCommand = new DelegateCommand(QuitGroup);
        EditGroupCommand = new DelegateCommand(EditGroup);
        GroupMemberCommand = new DelegateCommand(GroupMemberShow);
        InviteMemberCommand = new DelegateCommand(InviteMember);
        RemoveMemberCommand = new DelegateCommand(RemoveMember);
        CanDisturbCommand = new DelegateCommand(() =>
        {
            _eventAggregator.GetEvent<UpdateUnreadChatMessageCountEvent>().Publish();
        });
    }

    private void RemoveMember()
    {
        if (SelectedGroup == null) return;
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new RemoveGroupMemberViewModel(d, new DialogParameters
            {
                { "GroupRelationDto", SelectedGroup }
            }, null))
            .TryShow();
    }

    private void InviteMember()
    {
        if (SelectedGroup == null) return;
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new ShareViewModel(d, new DialogParameters
            {
                { "ShareMess", new CardMessDto { IsUser = false, Id = SelectedGroup.Id } },
                { "ShowMess", false }
            }, null))
            .TryShow();
    }

    private void GroupMemberShow()
    {
        _regionManager.RequestNavigate(nameof(GroupSideMemberView), new NavigationParameters
        {
            { "SelectedGroup", SelectedGroup }
        });
    }

    private void EditGroup()
    {
        _regionManager.RequestNavigate(nameof(GroupSideEditView), new NavigationParameters
        {
            { "SelectedGroup", SelectedGroup }
        });
    }

    /// <summary>
    /// 请求解散群聊
    /// </summary>
    private void DeleteGroup()
    {
        if (SelectedGroup == null) return;

        async void DeleteGroupCallback(IDialogResult result)
        {
            var groupService = _containerProvider.Resolve<IGroupService>();
            var res = await groupService.DisbandGroupRequest(_userManager.User?.Id!, SelectedGroup.Id!);
            if (res.Item1)
            {
                _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = $"您已解散群聊 {SelectedGroup.GroupDto?.Name ?? string.Empty}",
                    Type = NotificationType.Success
                });
            }
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定解散此群聊吗？", DeleteGroupCallback))
            .TryShow();
    }

    /// <summary>
    /// 请求退出群聊
    /// </summary>
    private void QuitGroup()
    {
        if (SelectedGroup == null) return;

        async void QuitGroupCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;
            var groupService = _containerProvider.Resolve<IGroupService>();
            await groupService.QuitGroupRequest(_userManager.User?.Id!, SelectedGroup.Id!);

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = $"您已退出群聊 {SelectedGroup.GroupDto?.Name ?? string.Empty}",
                Type = NotificationType.Success
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定退出此群聊吗？", QuitGroupCallback))
            .TryShow();
    }

    private void OnGroupMemberChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(GroupMembers));
    }

    #region INavigationAware

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedGroup = navigationContext.Parameters.GetValue<GroupRelationDto>("SelectedGroup");
        if (SelectedGroup.GroupDto != null)
            SelectedGroup.GroupDto.GroupMembers.CollectionChanged += OnGroupMemberChanged;
        _regionManager = navigationContext.NavigationService;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (SelectedGroup.GroupDto != null)
            SelectedGroup.GroupDto.GroupMembers.CollectionChanged -= OnGroupMemberChanged;
    }

    #endregion
}