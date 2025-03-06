using System;
using Avalonia.Collections;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using Prism.Commands;
using Prism.Ioc;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatRightTopPanelViewModel : ViewModelBase
{
    public ChatViewModel ChatViewModel { get; init; }

    // 聊天消息组
    private AvaloniaList<ChatData>? _chatMessages;

    public AvaloniaList<ChatData>? ChatMessages
    {
        get => _chatMessages;
        set => SetProperty(ref _chatMessages, value);
    }

    // 显示更多历史记录
    public DelegateCommand SearchMoreCommand { get; init; }

    // 点击头像事件
    public DelegateCommand<ChatData> HeadClickCommand { get; init; }

    // 文件消息点击事件
    public DelegateCommand<FileMessDto> FileMessageClickCommand { get; init; }

    public ChatRightTopPanelViewModel(ChatViewModel chatViewModel, IContainerProvider containerProvider)
    {
        ChatViewModel = chatViewModel;

        HeadClickCommand = new DelegateCommand<ChatData>(HeadClick);
        SearchMoreCommand = new DelegateCommand(ChatViewModel.SearchMoreFriendChatMessage);
        FileMessageClickCommand = new DelegateCommand<FileMessDto>(ChatViewModel.FileDownload);

        ChatViewModel.OnFriendSelectionChanged += () => { ChatMessages = ChatViewModel.SelectedFriend?.ChatMessages; };
    }

    private void HeadClick(ChatData obj)
    {
        //TODO: 转跳到好友详情页
        Console.WriteLine("HeadClick:" + obj.IsUser);
    }
}