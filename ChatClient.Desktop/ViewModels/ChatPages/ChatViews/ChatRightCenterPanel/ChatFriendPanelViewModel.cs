using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;

public class ChatFriendPanelViewModel : ViewModelBase, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    public ThemeStyle ThemeStyle { get; set; }

    public ChatInputPanelViewModel ChatInputPanelViewModel { get; private set; }

    private FriendChatDto? selectedFriend;

    public FriendChatDto? SelectedFriend
    {
        get => selectedFriend;
        set => SetProperty(ref selectedFriend, value);
    }

    public UserDto User => _userManager.User!;


    #region Command

    public DelegateCommand SearchMoreCommand { get; private set; }
    public DelegateCommand<ChatData> HeadClickCommand { get; private set; }
    public DelegateCommand<FileMessDto> FileMessageClickCommand { get; private set; }
    public DelegateCommand DeleteFriendCommand { get; private set; }

    #endregion

    public ChatFriendPanelViewModel(IContainerProvider containerProvider,
        IThemeStyle themeStyle,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        ThemeStyle = themeStyle.CurrentThemeStyle;

        ChatInputPanelViewModel = new ChatInputPanelViewModel(SendChatMessage, SendChatMessages, InputMessageChanged);

        HeadClickCommand = new DelegateCommand<ChatData>(HeadClick);
        SearchMoreCommand = new DelegateCommand(SearchMoreFriendChatMessage);
        FileMessageClickCommand = new DelegateCommand<FileMessDto>(FileDownload);
        DeleteFriendCommand = new DelegateCommand(DeleteFriend);
    }

    /// <summary>
    ///  处理输入事件
    /// </summary>
    /// <param name="obj"></param>
    private void InputMessageChanged(bool isInputing)
    {
        if (SelectedFriend == null) return;
        var chatService = _containerProvider.Resolve<IChatService>();
        chatService.SendFriendWritingMessage(_userManager.User!.Id, SelectedFriend.FriendRelatoinDto!.Id, isInputing);
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    private void DeleteFriend()
    {
        if (SelectedFriend == null) return;
        var friendService = _containerProvider.Resolve<IFriendService>();
        friendService.DeleteFriend(_userManager.User!.Id, SelectedFriend!.UserId);
    }

    private void HeadClick(ChatData obj)
    {
        //TODO: 转跳到好友详情页
        Console.WriteLine("HeadClick:" + obj.IsUser);
    }

    /// <summary>
    /// 加载更多历史记录
    /// </summary>
    public async void SearchMoreFriendChatMessage()
    {
        if (SelectedFriend == null) return;

        var chatMessages = SelectedFriend.ChatMessages;

        var chatPackService = _containerProvider.Resolve<IFriendChatPackService>();
        var chatDatas =
            await chatPackService.GetFriendChatDataAsync(_userManager.User?.Id, SelectedFriend.UserId,
                SelectedFriend.ChatMessages[0].ChatId,
                10);

        foreach (var chatData in chatDatas)
        {
            chatMessages.Insert(0, chatData);
            var duration = chatMessages[1].Time - chatData.Time;
            chatMessages[1].ShowTime = duration > TimeSpan.FromMinutes(5);
        }

        // 将最后一条消息的时间显示出来
        if (chatMessages.Count > 0)
            chatMessages[0].ShowTime = true;

        // 如果加载的消息不足15条，则说明没有更多消息了
        if (chatDatas.Count() != 10)
            SelectedFriend.HasMoreMessage = false;
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileMess"></param>
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
    public async void SendChatMessages(IEnumerable<object> messages)
    {
        if (SelectedFriend == null) return;

        var chatMessageDtos = messages.Select(message => message switch
        {
            string text when !string.IsNullOrEmpty(text) => new ChatMessageDto
            {
                Type = ChatMessage.ContentOneofCase.TextMess,
                Content = new TextMessDto { Text = text }
            },
            Bitmap bitmap => new ChatMessageDto
            {
                Type = ChatMessage.ContentOneofCase.ImageMess,
                Content = new ImageMessDto { ImageSource = bitmap, FileSize = bitmap.GetSize(), FilePath = "" }
            },
            _ => null
        }).Where(dto => dto != null).ToList();

        await SendChatMessagesInternal(chatMessageDtos!);
    }

    /// <summary>
    /// 处理单条聊天消息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="mess"></param>
    public async void SendChatMessage(ChatMessage.ContentOneofCase type, object mess)
    {
        if (SelectedFriend == null) return;

        var chatMessage = new ChatMessageDto { Type = type, Content = mess };
        await SendChatMessagesInternal([chatMessage]);
    }

    private async Task SendChatMessagesInternal(List<ChatMessageDto> chatMessageDtos)
    {
        if (SelectedFriend == null) return;

        bool showTime = SelectedFriend.ChatMessages.Count == 0 ||
                        DateTime.Now - SelectedFriend.ChatMessages.Last().Time > TimeSpan.FromMinutes(5);

        var chatData = new ChatData
        {
            ShowTime = showTime,
            IsUser = true,
            IsWriting = true,
            Time = DateTime.Now,
            ChatMessages = chatMessageDtos
        };

        SelectedFriend.ChatMessages.Add(chatData);

        var chatService = _containerProvider.Resolve<IChatService>();
        var (state, chatId) = await chatService.SendChatMessage(_userManager.User!.Id,
            SelectedFriend.FriendRelatoinDto!.Id,
            chatMessageDtos);

        chatData.IsWriting = false;
        chatData.IsError = !state;
        chatData.Time = DateTime.Now;
        chatData.ChatId = int.TryParse(chatId, out int id) ? id : 0;

        SelectedFriend.UpdateChatMessages();
    }

    #endregion

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedFriend = navigationContext.Parameters.GetValue<FriendChatDto>("SelectedFriend");
        ChatInputPanelViewModel.UpdateChatMessages(SelectedFriend.InputMessages);
        if (SelectedFriend is { FriendRelatoinDto: not null })
            SelectedFriend.FriendRelatoinDto.OnFriendRelationChanged += Friend_OnFriendRelationChanged;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (SelectedFriend != null)
        {
            var chatService = _containerProvider.Resolve<IChatService>();
            chatService.SendFriendWritingMessage(_userManager.User!.Id, SelectedFriend.FriendRelatoinDto!.Id, false);

            SelectedFriend.FriendRelatoinDto!.OnFriendRelationChanged -= Friend_OnFriendRelationChanged;
            SelectedFriend = null;
        }
    }

    private void Friend_OnFriendRelationChanged()
    {
        var friendService = _containerProvider.Resolve<IFriendService>();
        friendService.UpdateFriendRelation(_userManager.User!.Id, SelectedFriend!.FriendRelatoinDto!);
    }

    public void Destroy()
    {
        ChatInputPanelViewModel?.Dispose();
        SelectedFriend = null;
    }
}