using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class CreateGroupViewModel : BindableBase, IDialogAware
{
    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public AvaloniaList<FriendRelationDto> SelectedFriends { get; private set; } = new();


    public AvaloniaList<GroupFriendDto> GroupFriends { get; init; }

    public DelegateCommand OKCommand { get; set; }
    public DelegateCommand CancleCommand { get; set; }
    public DelegateCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; set; }

    public CreateGroupViewModel(IUserManager userManager, IContainerProvider containerProvider)
    {
        _userManager = userManager;
        _containerProvider = containerProvider;

        OKCommand = new DelegateCommand(CreateGroup, CanCreateGroup);
        CancleCommand = new DelegateCommand(() => RequestClose.Invoke(ButtonResult.Cancel));
        SelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(SelectionChanged);

        GroupFriends = userManager.GroupFriends!;
        GroupFriends.CollectionChanged += (sender, args) => OKCommand.RaiseCanExecuteChanged();
    }

    private bool CanCreateGroup() => GroupFriends.Count != 0;

    /// <summary>
    /// 创建群聊
    /// </summary>
    private async void CreateGroup()
    {
        List<string> friendIds = SelectedFriends.Select(d => d.Id).ToList();
        var groupService = _containerProvider.Resolve<IGroupService>();
        var result = await groupService.CreateGroup(_userManager.User!.Id, friendIds);
        if (result.Item1)
        {
            var dto = await _userManager.NewGroupReceive(result.Item2);
            RequestClose.Invoke(ButtonResult.OK);

            if (dto != null)
            {
                IEventAggregator eventAggregator = _containerProvider.Resolve<IEventAggregator>();
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

    #region Dialog

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}