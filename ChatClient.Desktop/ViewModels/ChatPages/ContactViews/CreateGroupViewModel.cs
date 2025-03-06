using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class CreateGroupViewModel : BindableBase, IDialogAware
{
    private readonly IUserManager _userManager;

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

    public CreateGroupViewModel(IUserManager userManager)
    {
        _userManager = userManager;

        GroupFriends = userManager.GroupFriends!;

        OKCommand = new DelegateCommand(() => RequestClose.Invoke(ButtonResult.OK));
        CancleCommand = new DelegateCommand(() => RequestClose.Invoke(ButtonResult.Cancel));
        SelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(SelectionChanged);
    }

    private void SelectionChanged(SelectionChangedEventArgs args)
    {
        if (args.AddedItems != null && args.AddedItems.Count != 0)
        {
            SelectedFriends.AddRange(args.AddedItems.Cast<FriendRelationDto>());
        }

        if (args.RemovedItems != null && args.RemovedItems.Count != 0)
        {
            foreach (var item in args.RemovedItems)
            {
                var friend = item as FriendRelationDto;
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