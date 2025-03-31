using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Controls.Experimental;
using SukiUI.Dialogs;
using ChatMessage = ChatServer.Common.Protobuf.ChatMessage;

namespace ChatClient.Desktop.ViewModels.ShareView;

public class ShareViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    private readonly Action<IDialogResult>? _requestClose;

    private readonly IUserManager _userManager;

    public AvaloniaList<GroupFriendDto> GroupFriends { get; private set; }
    public AvaloniaList<GroupGroupDto> GroupGroups { get; private set; }

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private object _shareMess;

    public object ShareMess
    {
        get => _shareMess;
        set => SetProperty(ref _shareMess, value);
    }

    private string _senderMessage = string.Empty;

    public string SenderMessage
    {
        get => _senderMessage;
        set => SetProperty(ref _senderMessage, value);
    }

    public AvaloniaList<object> SelectedRelations { get; private set; } = new();

    public AsyncDelegateCommand OKCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }
    public DelegateCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; set; }

    /// <summary>
    /// "ShareMess" : 需要分享的消息
    /// </summary>
    /// <param name="sukiDialog"></param>
    /// <param name="parameters"></param>
    /// <param name="requestClose"></param>
    public ShareViewModel(ISukiDialog sukiDialog, IDialogParameters parameters, Action<IDialogResult>? requestClose)
    {
        _sukiDialog = sukiDialog;
        _requestClose = requestClose;

        _userManager = App.Current.Container.Resolve<IUserManager>();

        ShareMess = parameters.GetValue<object>("ShareMess");

        OKCommand = new AsyncDelegateCommand(ShareMessage, CanShareMessage);
        CancelCommand = new DelegateCommand(() =>
        {
            _sukiDialog.Dismiss();
            _requestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        });
        SelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(SelectionChanged);

        GroupFriends = _userManager.GroupFriends!;
        GroupGroups = _userManager.GroupGroups!;
        SelectedRelations.CollectionChanged += (sender, args) => OKCommand.RaiseCanExecuteChanged();
    }

    private void SelectionChanged(SelectionChangedEventArgs args)
    {
        if (args.AddedItems != null && args.AddedItems.Count != 0)
            SelectedRelations.AddRange(args.AddedItems.Cast<Control>().Select(d => d.DataContext));

        if (args.RemovedItems != null && args.RemovedItems.Count != 0)
        {
            foreach (var item in args.RemovedItems)
                SelectedRelations.Remove(((Control)item).DataContext);
        }
    }

    private bool CanShareMessage() => SelectedRelations.Count != 0;

    private async Task ShareMessage()
    {
        var type = ShareMess switch
        {
            TextMessDto _ => ChatMessage.ContentOneofCase.TextMess,
            CardMessDto _ => ChatMessage.ContentOneofCase.CardMess,
            FileMessDto _ => ChatMessage.ContentOneofCase.FileMess,
            ImageMessDto _ => ChatMessage.ContentOneofCase.ImageMess,
            _ => ChatMessage.ContentOneofCase.None
        };
        if (type == ChatMessage.ContentOneofCase.None) return;

        // 发送消息
        var chatService = App.Current.Container.Resolve<IChatService>();
        var result = await chatService.SendChatShareMessage(_userManager.User.Id,
            new ChatMessageDto { Content = ShareMess, Type = type },
            SenderMessage, SelectedRelations);

        var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        if (result)
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = type == ChatMessage.ContentOneofCase.CardMess ? "分享成功" : "转发成功",
                Type = NotificationType.Success
            });
        else
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = type == ChatMessage.ContentOneofCase.CardMess ? "分享失败" : "转发失败",
                Type = NotificationType.Error
            });

        _requestClose?.Invoke(new DialogResult(ButtonResult.OK));
        _sukiDialog.Dismiss();
    }
}