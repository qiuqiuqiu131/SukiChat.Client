using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class FriendChatMessageBoxViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;
    private CancellationTokenSource? CancellationTokenSource;

    private FriendRelationDto _friendRelation;

    public FriendRelationDto FriendRelation
    {
        get => _friendRelation;
        set => SetProperty(ref _friendRelation, value);
    }

    private ChatData _chatData;

    public ChatData ChatData
    {
        get => _chatData;
        set => SetProperty(ref _chatData, value);
    }


    public DelegateCommand ClickCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public FriendChatMessageBoxViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        ClickCommand = new DelegateCommand(Click);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());
    }

    private async void Click()
    {
        if (CancellationTokenSource != null)
            await CancellationTokenSource.CancelAsync();

        RequestClose.Invoke();
        Task.Run(async () =>
        {
            await Task.Delay(50);
            Dispatcher.UIThread.Post(async () =>
            {
                _eventAggregator.GetEvent<ShowWindowEvent>().Publish();
                await Task.Delay(50);
                _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
                _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(FriendRelation);
            });
        });
    }


    #region Dialog

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        ChatData = parameters.GetValue<ChatData>("ChatData");
        FriendRelation = parameters.GetValue<FriendRelationDto>("Dto");

        CancellationTokenSource = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromSeconds(5), CancellationTokenSource.Token)
            .ContinueWith(d =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (d.IsCompleted)
                        RequestClose.Invoke();
                });
            }, CancellationTokenSource.Token);
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}