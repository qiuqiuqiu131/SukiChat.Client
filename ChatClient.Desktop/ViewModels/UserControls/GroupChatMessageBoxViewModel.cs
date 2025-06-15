using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class GroupChatMessageBoxViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;
    private CancellationTokenSource? CancellationTokenSource;

    private GroupChatData _chatData;

    public GroupChatData ChatData
    {
        get => _chatData;
        private set => SetProperty(ref _chatData, value);
    }

    private GroupRelationDto _groupRelationDto;

    public GroupRelationDto GroupRelation
    {
        get => _groupRelationDto;
        private set => SetProperty(ref _groupRelationDto, value);
    }

    public DelegateCommand ClickCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public GroupChatMessageBoxViewModel(IEventAggregator eventAggregator)
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
                _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(GroupRelation);
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
        ChatData = parameters.GetValue<GroupChatData>("ChatData");
        GroupRelation = parameters.GetValue<GroupRelationDto>("Dto");

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