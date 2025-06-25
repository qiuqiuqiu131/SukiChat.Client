using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;

public class CreateGroupViewModel : BindableBase
{
    private readonly ISukiDialog _dialog;
    private readonly Action<IDialogResult>? RequestClose;

    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public AvaloniaList<FriendRelationDto> SelectedFriends { get; private set; } = new();

    public AvaloniaList<GroupFriendDto> GroupFriends { get; init; }

    public AsyncDelegateCommand OKCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }
    public DelegateCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; set; }

    public CreateGroupViewModel(ISukiDialog dialog, Action<IDialogResult>? requestClose)
    {
        _containerProvider = App.Current.Container.Resolve<IContainerProvider>();
        _userManager = _containerProvider.Resolve<IUserManager>();
        _eventAggregator = _containerProvider.Resolve<IEventAggregator>();

        _dialog = dialog;
        RequestClose = requestClose;

        OKCommand = new AsyncDelegateCommand(CreateGroup, CanCreateGroup);
        CancelCommand = new DelegateCommand(() =>
        {
            _dialog.Dismiss();
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        });
        SelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(SelectionChanged);

        GroupFriends = _userManager.GroupFriends!;
        SelectedFriends.CollectionChanged += (sender, args) => OKCommand.RaiseCanExecuteChanged();
    }

    private bool CanCreateGroup() => SelectedFriends.Count >= 2;

    /// <summary>
    /// 创建群聊
    /// </summary>
    private async Task CreateGroup()
    {
        List<string> friendIds = SelectedFriends.Select(d => d.Id).ToList();
        var groupService = _containerProvider.Resolve<IGroupService>();
        var result = await groupService.CreateGroup(_userManager.User!.Id, friendIds);
        if (result.Item1)
        {
            var dto = await _userManager.NewGroupReceive(result.Item2);

            _dialog.Dismiss();
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));

            if (dto != null)
            {
                IEventAggregator eventAggregator = _containerProvider.Resolve<IEventAggregator>();
                _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
                eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(dto);
            }
        }
    }

    private void SelectionChanged(SelectionChangedEventArgs args)
    {
        if (args.AddedItems != null && args.AddedItems.Count != 0)
        {
            SelectedFriends.AddRange(args.AddedItems.Cast<Control>().Select(d => d.DataContext)
                .Cast<FriendRelationDto>());
        }

        if (args.RemovedItems != null && args.RemovedItems.Count != 0)
        {
            foreach (var item in args.RemovedItems)
            {
                var friend = ((Control)item).DataContext as FriendRelationDto;
                SelectedFriends.Remove(friend!);
            }
        }
    }
}