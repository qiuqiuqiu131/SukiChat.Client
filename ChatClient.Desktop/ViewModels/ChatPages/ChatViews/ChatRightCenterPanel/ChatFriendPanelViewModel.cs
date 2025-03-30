using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;

public class ChatFriendPanelViewModel : ViewModelBase, IDestructible, IRegionMemberLifetime
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _sukiDialogManager;
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
    public AsyncDelegateCommand<FileMessDto> FileMessageClickCommand { get; private set; }
    public AsyncDelegateCommand<FileMessDto> FileRestoreCommand { get; private set; }
    public AsyncDelegateCommand DeleteFriendCommand { get; private set; }

    #endregion

    public ChatFriendPanelViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        ISukiDialogManager sukiDialogManager,
        IThemeStyle themeStyle,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _sukiDialogManager = sukiDialogManager;
        _userManager = userManager;

        ThemeStyle = themeStyle.CurrentThemeStyle;

        ChatInputPanelViewModel = new ChatInputPanelViewModel(SendChatMessage, SendChatMessages, InputMessageChanged);

        SearchMoreCommand = new DelegateCommand(SearchMoreFriendChatMessage);
        FileRestoreCommand = new AsyncDelegateCommand<FileMessDto>(FileRestoreDownload);
        FileMessageClickCommand = new AsyncDelegateCommand<FileMessDto>(FileDownload);
        DeleteFriendCommand = new AsyncDelegateCommand(DeleteFriend);
    }

    /// <summary>
    ///  处理输入事件
    /// </summary>
    /// <param name="isInputing"></param>
    private void InputMessageChanged(bool isInputing)
    {
        if (SelectedFriend == null) return;
        var chatService = _containerProvider.Resolve<IChatService>();
        chatService.SendFriendWritingMessage(_userManager.User.Id, SelectedFriend.FriendRelatoinDto.Id, isInputing);
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    private async Task DeleteFriend()
    {
        if (SelectedFriend == null) return;

        async void DeleteFriendCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;
            var friendService = _containerProvider.Resolve<IFriendService>();
            await friendService.DeleteFriend(_userManager.User!.Id, SelectedFriend.UserId);

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = $"您已删除好友 {SelectedFriend.FriendRelatoinDto?.UserDto?.Name ?? string.Empty}",
                Type = NotificationType.Success
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定删除好友吗？", DeleteFriendCallback))
            .TryShow();
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
            chatMessages[1].ShowTime = duration > TimeSpan.FromMinutes(3);
        }

        // 将最后一条消息的时间显示出来
        if (chatMessages.Count > 0)
            chatMessages[0].ShowTime = true;

        // 如果加载的消息不足15条，则说明没有更多消息了
        if (chatDatas.Count() != 10)
            SelectedFriend.HasMoreMessage = false;
    }

    #region DownloadFile

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileMess"></param>
    public async Task FileDownload(FileMessDto fileMess)
    {
        // string filePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // filePath = Path.Combine(filePath, "Downloads");
        var appDataManager = _containerProvider.Resolve<IAppDataManager>();
        var filePath = appDataManager
            .GetFileInfo(Path.Combine("Users", fileMess.IsUser ? _userManager.User.Id : SelectedFriend.UserId,
                "ChatFile", fileMess.FileName)).FullName;

        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 记录另存文件路径
        fileMess.TargetFilePath = filePath;

        fileMess.IsDownloading = true;
        var progress = new Progress<double>(d => { fileMess.DownloadProgress = d; });

        // 开始下载文件
        var fileIOHelper = _containerProvider.Resolve<IFileIOHelper>();
        var path = Path.Combine("Users", fileMess.IsUser ? _userManager.User.Id : SelectedFriend!.UserId, "ChatFile");
        var result =
            await fileIOHelper.DownloadLargeFileAsync(path, fileMess.FileName, filePath, progress);

        // 记录文件下载完毕
        fileMess.IsDownloading = false;
        fileMess.IsSuccess = result;
        fileMess.IsExit = result;

        var chatService = _containerProvider.Resolve<IChatService>();
        await chatService.UpdateFileMess(_userManager.User.Id, fileMess, FileTarget.User);

        if (result)
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "文件下载成功",
                Type = NotificationType.Success
            });
        }
        else
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "文件下载失败",
                Type = NotificationType.Error
            });
        }
    }

    /// <summary>
    /// 另存文件
    /// </summary>
    /// <param name="fileMess"></param>
    public async Task FileRestoreDownload(FileMessDto fileMess)
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

        // 开始下载文件
        var fileOperateHelper = _containerProvider.Resolve<IFileOperateHelper>();
        var result =
            await fileOperateHelper.SaveAsFile(fileMess.IsUser ? _userManager.User.Id : SelectedFriend.UserId,
                "ChatFile", fileMess.FileName, filePath,
                FileTarget.User);

        if (result)
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "文件下载成功",
                Type = NotificationType.Success
            });
        }
        else
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "文件下载失败",
                Type = NotificationType.Error
            });
        }
    }

    #endregion


    #region SendChatMessage

    /// <summary>
    /// 处理多成分聊天消息
    /// </summary>
    /// <param name="messages"></param>
    public async Task<(bool, FileTarget)> SendChatMessages(IEnumerable<object> messages)
    {
        if (SelectedFriend == null) return (false, FileTarget.User);

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

        return (await SendChatMessagesInternal(chatMessageDtos!), FileTarget.User);
    }

    /// <summary>
    /// 处理单条聊天消息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="mess"></param>
    public async Task<(bool, FileTarget)> SendChatMessage(ChatMessage.ContentOneofCase type, object mess)
    {
        if (SelectedFriend == null) return (false, FileTarget.User);

        var chatMessage = new ChatMessageDto { Type = type, Content = mess };
        return (await SendChatMessagesInternal([chatMessage]), FileTarget.User);
    }

    private async Task<bool> SendChatMessagesInternal(List<ChatMessageDto> chatMessageDtos)
    {
        if (SelectedFriend == null) return false;

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

        return state;
    }

    #endregion

    #region Region

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
            chatService.SendFriendWritingMessage(_userManager.User!.Id, SelectedFriend.UserId, false);

            if (SelectedFriend.FriendRelatoinDto != null)
                SelectedFriend.FriendRelatoinDto.OnFriendRelationChanged -= Friend_OnFriendRelationChanged;
            SelectedFriend = null;
        }
    }

    private void Friend_OnFriendRelationChanged(FriendRelationDto dto)
    {
        var friendService = _containerProvider.Resolve<IFriendService>();
        friendService.UpdateFriendRelation(_userManager.User!.Id, dto);
    }

    public void Destroy()
    {
        ChatInputPanelViewModel?.Dispose();
        SelectedFriend = null;
    }

    public bool KeepAlive => false;

    #endregion
}