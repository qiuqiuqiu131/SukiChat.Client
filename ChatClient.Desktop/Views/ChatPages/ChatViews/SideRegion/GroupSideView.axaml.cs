using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ChatClient.BaseService.Manager;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Events;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.SideRegion;

public partial class GroupSideView : UserControl
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IEventAggregator _eventAggregator;

    public GroupSideView(IUserDtoManager userDtoManager, IEventAggregator eventAggregator)
    {
        _userDtoManager = userDtoManager;
        _eventAggregator = eventAggregator;
        InitializeComponent();
    }

    private async void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: GroupMemberDto groupMemberDto })
        {
            e.Source = sender;
            var userDto = await _userDtoManager.GetUserDto(groupMemberDto.UserId);
            _eventAggregator.GetEvent<UserMessageBoxShowEvent>()
                .Publish(new UserMessageBoxShowArgs(userDto, e));
        }
    }
}