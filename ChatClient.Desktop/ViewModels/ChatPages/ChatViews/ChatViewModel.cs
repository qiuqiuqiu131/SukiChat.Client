using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Material.Icons;
using Prism.Ioc;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatViewModel : ChatPageBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    #region PanelViewModels

    public ChatLeftPanelViewModel ChatLeftPanelViewModel { get; init; }
    public ChatRightTopPanelViewModel ChatRightTopPanelViewModel { get; init; }
    public ChatRightBottomPanelViewModel ChatRightBottomPanelViewModel { get; init; }

    #endregion

    #region SelectedFriend

    private FriendChatDto? _selectedFriend;

    public FriendChatDto? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            SetProperty(ref _selectedFriend, value);
            RaisePropertyChanged(nameof(IsFriendSelected));
        }
    }

    private FriendChatDto? previousSelectedFriend { get; set; }

    #endregion

    public UserDto? User => _userManager.User;
    public AvaloniaList<FriendChatDto> Friends => _userManager.FriendChats!;

    public bool IsFriendSelected => SelectedFriend != null;

    public event Action? OnFriendSelectionChanged;

    public ChatViewModel(IContainerProvider containerProvider,
        IUserManager userManager) : base("聊天", MaterialIconKind.Chat, 0)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        // 生成面板VM
        ChatLeftPanelViewModel = new ChatLeftPanelViewModel(this, containerProvider);
        ChatRightBottomPanelViewModel = new ChatRightBottomPanelViewModel(this, containerProvider);
        ChatRightTopPanelViewModel = new ChatRightTopPanelViewModel(this, containerProvider);
    }

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async void FriendSelectionChanged(FriendChatDto? friendChatDto)
    {
        if (friendChatDto == null || friendChatDto == SelectedFriend) return;

        var chatService = _containerProvider.Resolve<IChatService>();

        // 处理上一个选中的好友
        if (previousSelectedFriend != null && previousSelectedFriend.ChatMessages.Count > 15)
        {
            previousSelectedFriend.HasMoreMessage = true;
            // 将PreviousSelectedFriend的聊天记录裁剪到只剩15条
            previousSelectedFriend.ChatMessages.RemoveRange(0, previousSelectedFriend.ChatMessages.Count - 15);

            // 发送好友停止输入消息
            _ = chatService.SendFriendWritingMessage(User?.Id, previousSelectedFriend.UserId, false);
        }

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (friendChatDto.ChatMessages.Count == 0)
            friendChatDto.HasMoreMessage = false;
        else if (friendChatDto.ChatMessages.Count == 1)
        {
            var chatPackService = _containerProvider.Resolve<IChatPackService>();
            var chatDatas =
                await chatPackService.GetChatDataAsync(User?.Id, friendChatDto.UserId,
                    friendChatDto.ChatMessages[0].ChatId,
                    15);

            foreach (var chatData in chatDatas)
            {
                friendChatDto.ChatMessages.Insert(0, chatData);
                var duration = friendChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(5))
                    friendChatDto.ChatMessages[1].ShowTime = true;
                else
                    friendChatDto.ChatMessages[1].ShowTime = false;
            }

            if (chatDatas.Count() != 15)
                friendChatDto.HasMoreMessage = false;
        }

        // 将最后一条消息的时间显示出来
        if (friendChatDto.ChatMessages.Count > 0)
            friendChatDto.ChatMessages[0].ShowTime = true;

        friendChatDto.UnReadMessageCount = 0;
        _ = chatService.ReadAllChatMessage(User!.Id, friendChatDto.UserId);

        SelectedFriend = friendChatDto;
        previousSelectedFriend = SelectedFriend;
        OnFriendSelectionChanged?.Invoke();
    }

    /// <summary>
    /// 加载更多历史记录
    /// </summary>
    public async void SearchMoreFriendChatMessage()
    {
        if (SelectedFriend == null) return;

        var chatMessages = SelectedFriend.ChatMessages;

        var chatPackService = _containerProvider.Resolve<IChatPackService>();
        var chatDatas =
            await chatPackService.GetChatDataAsync(User?.Id, SelectedFriend.UserId,
                SelectedFriend.ChatMessages[0].ChatId,
                15);

        foreach (var chatData in chatDatas)
        {
            chatMessages.Insert(0, chatData);
            var duration = chatMessages[1].Time - chatData.Time;
            if (duration > TimeSpan.FromMinutes(5))
                chatMessages[1].ShowTime = true;
            else
                chatMessages[1].ShowTime = false;
        }

        // 将最后一条消息的时间显示出来
        if (chatMessages.Count > 0)
            chatMessages[0].ShowTime = true;

        // 如果加载的消息不足15条，则说明没有更多消息了
        if (chatDatas.Count() != 15)
            SelectedFriend.HasMoreMessage = false;
    }

    public async void FileDownload(FileMessDto fileMess)
    {
        // 获取文件地址
        string filePath = "";
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            var handle = window!.TryGetPlatformHandle()?.Handle;

            if (handle == null) return;

            filePath = await SystemFileDialog.SaveFileAsync(handle.Value, fileMess.FileName, "保存文件",
                $"{fileMess.FileType}\0*{fileMess.FileType}\0");
        }

        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 记录另存文件路径
        fileMess.TargetFilePath = filePath;
        var chatService1 = _containerProvider.Resolve<IChatService>();
        await chatService1.UpdateFileMess(fileMess);

        fileMess.FileProcessDto = new FileProcessDto
        {
            CurrentSize = 0,
            MaxSize = fileMess.FileSize
        };

        // 开始下载文件
        var fileIOHelper = _containerProvider.Resolve<IFileIOHelper>();
        var path = Path.Combine("Users", SelectedFriend!.UserId, "ChatFile");
        var result =
            await fileIOHelper.DownloadLargeFileAsync(path, fileMess.FileName, filePath, fileMess.FileProcessDto);

        fileMess.FileProcessDto = null;

        // 记录文件下载完毕
        fileMess.IsDownload = result;
        var chatService2 = _containerProvider.Resolve<IChatService>();
        await chatService2.UpdateFileMess(fileMess);
    }

    #region SendChatMessage

    /// <summary>
    /// 处理多成分聊天消息
    /// </summary>
    /// <param name="messages"></param>
    public async Task SendChatMessages(IEnumerable<object> messages)
    {
        if (SelectedFriend == null) return;

        // 转换消息
        List<ChatMessageDto> chatMessageDtos = new();
        foreach (var message in messages)
        {
            if (message is string text && !string.IsNullOrEmpty(text))
            {
                var messDto = new ChatMessageDto
                {
                    Type = ChatMessage.ContentOneofCase.TextMess,
                    Content = new TextMessDto { Text = text }
                };
                chatMessageDtos.Add(messDto);
            }
            else if (message is Bitmap bitmap)
            {
                var messDto = new ChatMessageDto
                {
                    Type = ChatMessage.ContentOneofCase.ImageMess,
                    Content = new ImageMessDto { ImageSource = bitmap, FileSize = bitmap.GetSize(), FilePath = "" }
                };
                chatMessageDtos.Add(messDto);
            }
        }

        bool showTime = true;
        if (SelectedFriend.ChatMessages.Count > 0)
        {
            var last = SelectedFriend.ChatMessages.Last();
            var currentTime = DateTime.Now;
            showTime = currentTime - last.Time > TimeSpan.FromMinutes(5);
        }

        var chatData = new ChatData
        {
            ShowTime = showTime,
            IsUser = true,
            IsWriting = true,
            Time = DateTime.Now,
            ChatMessages = chatMessageDtos
        };

        SelectedFriend.ChatMessages.Add(chatData);

        // 发送聊天消息
        var chatService = _containerProvider.Resolve<IChatService>();
        var (state, _) =
            await chatService.SendChatMessage(_userManager.User!.Id, SelectedFriend.FriendRelatoinDto!.Id,
                chatMessageDtos);

        if (state)
        {
            chatData.IsWriting = false;
            chatData.IsError = false;
            chatData.Time = DateTime.Now;
        }
        else
        {
            chatData.IsWriting = false;
            chatData.IsError = true;
            chatData.Time = DateTime.Now;
        }

        SelectedFriend.UpdateChatMessages();
    }

    /// <summary>
    /// 处理单条聊天消息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="mess"></param>
    public async Task SendChatMessage(ChatMessage.ContentOneofCase type, object mess)
    {
        if (SelectedFriend == null) return;

        var chatMessage = new ChatMessageDto
        {
            Type = type,
            Content = mess
        };

        bool showTime = true;
        if (SelectedFriend.ChatMessages.Count > 0)
        {
            var last = SelectedFriend.ChatMessages.Last();
            var currentTime = DateTime.Now;
            showTime = currentTime - last.Time > TimeSpan.FromMinutes(5);
        }

        var chatData = new ChatData
        {
            ShowTime = showTime,
            IsUser = true,
            IsWriting = true,
            ChatMessages = [chatMessage],
            Time = DateTime.Now
        };

        // 添加到聊天记录
        SelectedFriend.ChatMessages.Add(chatData);

        // 发送聊天消息
        var chatService = _containerProvider.Resolve<IChatService>();
        var (state, _) =
            await chatService.SendChatMessage(_userManager.User!.Id, SelectedFriend.FriendRelatoinDto!.Id,
                [chatMessage]);

        if (state)
        {
            chatData.IsWriting = false;
            chatData.IsError = false;
            chatData.Time = DateTime.Now;
        }
        else
        {
            chatData.IsWriting = false;
            chatData.IsError = true;
            chatData.Time = DateTime.Now;
        }

        SelectedFriend.UpdateChatMessages();
    }

    #endregion
}