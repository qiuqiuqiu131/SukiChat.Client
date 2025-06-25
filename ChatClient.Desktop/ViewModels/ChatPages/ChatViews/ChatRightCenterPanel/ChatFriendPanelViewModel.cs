using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatClient.Avalonia.Common;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.CallView;
using ChatClient.Desktop.Views.ChatPages.ChatViews.SideRegion;
using ChatClient.Tool.Config;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Media.Call;
using ChatClient.Tool.Tools;
using ChatClient.Tool.UIEntity;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;

[RegionMemberLifetime(KeepAlive = true)]
public class ChatFriendPanelViewModel : ViewModelBase, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly AppSettings _appSettings;
    private readonly IUserManager _userManager;

    private SubscriptionToken? _token;

    public IRegionManager RegionManager { get; }

    public ThemeStyle ThemeStyle { get; set; }

    public ChatInputPanelViewModel ChatInputPanelViewModel { get; private set; }

    private FriendChatDto? selectedFriend;

    public FriendChatDto? SelectedFriend
    {
        get => selectedFriend;
        set => SetProperty(ref selectedFriend, value);
    }

    private bool sendMessageButtonVisible = true;

    public bool SendMessageButtonVisible
    {
        get => sendMessageButtonVisible;
        set => SetProperty(ref sendMessageButtonVisible, value);
    }

    public UserDto User => _userManager.User.UserDto!;


    #region Command

    public DelegateCommand<ChatData> DeleteMessageCommand { get; private set; }
    public DelegateCommand<ChatData> RetractMessageCommand { get; private set; }
    public DelegateCommand<object> ShareMessageCommand { get; private set; }
    public DelegateCommand SearchMoreCommand { get; private set; }
    public AsyncDelegateCommand<FileMessDto> FileMessageClickCommand { get; private set; }
    public AsyncDelegateCommand<object> FileRestoreCommand { get; private set; }
    public AsyncDelegateCommand VoiceCallCommand { get; private set; }
    public AsyncDelegateCommand VideoCallCommand { get; private set; }
    public AsyncDelegateCommand<CallMessDto> ReCallCommand { get; private set; }

    #endregion

    public ChatFriendPanelViewModel(IContainerProvider containerProvider,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ISukiDialogManager sukiDialogManager,
        AppSettings appSettings,
        IThemeStyle themeStyle,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _sukiDialogManager = sukiDialogManager;
        _appSettings = appSettings;
        _userManager = userManager;

        RegionManager = regionManager.CreateRegionManager();
        RegionManager.RegisterViewWithRegion(RegionNames.ChatSideRegion, typeof(FriendSideView));

        ThemeStyle = themeStyle.CurrentThemeStyle;

        ChatInputPanelViewModel = new ChatInputPanelViewModel(SendChatMessage, SendChatMessages, InputMessageChanged,
            SendChatMessageVisible: b => SendMessageButtonVisible = b);

        DeleteMessageCommand = new DelegateCommand<ChatData>(DeleteMessage);
        RetractMessageCommand = new DelegateCommand<ChatData>(RetractMessage);
        ShareMessageCommand = new DelegateCommand<object>(ShareMessage);
        SearchMoreCommand = new DelegateCommand(SearchMoreFriendChatMessage);
        FileRestoreCommand = new AsyncDelegateCommand<object>(FileRestoreDownload);
        FileMessageClickCommand = new AsyncDelegateCommand<FileMessDto>(FileDownload);
        VoiceCallCommand = new AsyncDelegateCommand(VoiceCall);
        VideoCallCommand = new AsyncDelegateCommand(VideoCall);
        ReCallCommand = new AsyncDelegateCommand<CallMessDto>(ReCall);
    }

    private async Task ReCall(CallMessDto obj)
    {
        if (obj.IsTelephone)
            await VoiceCall();
        else
            await VideoCall();
    }

    /// <summary>
    /// 视频通话
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private async Task VideoCall()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "暂不支持非Windows平台的视频通话",
                Type = NotificationType.Warning
            });
            return;
        }

        var exist = ChatCallHelper.OpenCallDialog(SelectedFriend!.UserId);
        if (exist) return;

        // 关闭其他通话窗口
        ChatCallHelper.CloseOtherCallDialog();

        var callManager = _containerProvider.Resolve<ICallManager>();
        var callOperator = await callManager.StartCall(CallType.Video);

        var dialogService = _containerProvider.Resolve<IDialogService>();
        dialogService.Show(nameof(VideoCallView), new DialogParameters
        {
            { "callOperator", callOperator },
            { "sender", SelectedFriend.FriendRelatoinDto }
        }, null, nameof(SukiCallDialogWindow));
    }

    /// <summary>
    /// 语音通话
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private async Task VoiceCall()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "暂不支持非Windows平台的语音通话",
                Type = NotificationType.Warning
            });
            return;
        }

        var exist = ChatCallHelper.OpenCallDialog(SelectedFriend!.UserId);
        if (exist) return;

        // 关闭其他通话窗口
        ChatCallHelper.CloseOtherCallDialog();

        var callManager = _containerProvider.Resolve<ICallManager>();
        var callOperator = await callManager.StartCall(CallType.Telephone);

        var dialogService = _containerProvider.Resolve<IDialogService>();
        dialogService.Show(nameof(CallView), new DialogParameters
        {
            { "callOperator", callOperator },
            { "sender", SelectedFriend.FriendRelatoinDto }
        }, null, nameof(SukiCallDialogWindow));
    }


    /// <summary>
    /// 转发消息
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ShareMessage(object obj)
    {
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new ShareViewModel(d, new DialogParameters { { "ShareMess", obj } }, null))
            .TryShow();
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
    /// 撤回消息
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void RetractMessage(ChatData obj)
    {
        if (SelectedFriend == null) return;

        if (DateTime.Now - obj.Time > TimeSpan.FromMinutes(2))
        {
            _sukiDialogManager.CreateDialog()
                .WithViewModel(d => new CommonDialogViewModel(d, "消息已超过2分钟，无法撤回", null))
                .TryShow();
        }
        else
        {
            async void RetractMessageCallback(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK) return;

                var chatService = _containerProvider.Resolve<IChatService>();
                var response = await chatService.RetractChatMessage(_userManager.User.Id, obj.ChatId, FileTarget.User);

                if (response)
                {
                    _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                    {
                        Message = "消息撤回成功",
                        Type = NotificationType.Success
                    });
                }
                else
                {
                    _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                    {
                        Message = "消息撤回失败",
                        Type = NotificationType.Error
                    });
                }
            }

            _sukiDialogManager.CreateDialog()
                .WithViewModel(d => new CommonDialogViewModel(d, "确定撤回此消息吗？", RetractMessageCallback))
                .TryShow();
        }
    }

    /// <summary>
    /// 删除聊天消息
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void DeleteMessage(ChatData obj)
    {
        if (SelectedFriend == null) return;

        async void DeleteMessageCallback(IDialogResult dialogResult)
        {
            if (dialogResult.Result != ButtonResult.OK) return;

            var index = SelectedFriend.ChatMessages.IndexOf(obj);
            SelectedFriend.ChatMessages.Remove(obj);
            if (SelectedFriend.ChatMessages.Count > index && index > 0)
            {
                var last = SelectedFriend.ChatMessages[index];
                if (last.Time - SelectedFriend.ChatMessages[index - 1].Time > TimeSpan.FromMinutes(3))
                    last.ShowTime = true;
                else
                    last.ShowTime = false;
            }
            else if (SelectedFriend.ChatMessages.Count > index)
                SelectedFriend.ChatMessages[0].ShowTime = true;

            var chatService = _containerProvider.Resolve<IChatService>();
            await chatService.DeleteChatMessage(_userManager.User!.Id, obj.ChatId, FileTarget.User);

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "聊天消息已删除",
                Type = NotificationType.Information
            });
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new CommonDialogViewModel(d, "确定删除此聊天消息吗？", DeleteMessageCallback))
            .TryShow();
    }

    /// <summary>
    /// 加载更多历史记录
    /// </summary>
    public async void SearchMoreFriendChatMessage()
    {
        if (SelectedFriend == null) return;

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        using var scopeProvider = _containerProvider.CreateScope();
        var chatPackService = scopeProvider.Resolve<IFriendChatPackService>();

        int nextCount = _appSettings.ChatMessageCount;
        var chatDatas =
            await chatPackService.GetFriendChatDataAsync(User?.Id, SelectedFriend.UserId,
                SelectedFriend.ChatMessages[0].ChatId,
                nextCount);

        if (chatDatas.Count != nextCount)
            SelectedFriend.HasMoreMessage = false;
        else
            SelectedFriend.HasMoreMessage = true;

        float value = nextCount;
        foreach (var chatData in chatDatas)
        {
            if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.ImageMess))
                value -= 2f;
            else if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.FileMess))
                value -= 2f;
            else
                value -= 1;

            if (value <= 0) break;

            SelectedFriend.ChatMessages.Insert(0, chatData);
            var duration = SelectedFriend.ChatMessages[1].Time - chatData.Time;
            if (duration > TimeSpan.FromMinutes(3))
                SelectedFriend.ChatMessages[1].ShowTime = true;
            else
                SelectedFriend.ChatMessages[1].ShowTime = false;
        }

        // 将最后一条消息的时间显示出来
        if (SelectedFriend.ChatMessages.Count > 0)
        {
            SelectedFriend.ChatMessages[0].ShowTime = true;

            SelectedFriend.UnReadMessageCount = 0;

            var maxChatId = SelectedFriend.ChatMessages.Max(d => d.ChatId);

            var chatService = _containerProvider.Resolve<IChatService>();
            _ = chatService.ReadAllChatMessage(User!.Id, SelectedFriend.UserId, maxChatId, FileTarget.User);
        }
    }

    #region DownloadFile

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileMess"></param>
    public async Task FileDownload(FileMessDto fileMess)
    {
        if (fileMess.IsSuccess) return;

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
    public async Task FileRestoreDownload(object mess)
    {
        if (mess is FileMessDto fileMess)
        {
            // 获取文件地址
            string filePath = "";
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow;
                var handle = window!.TryGetPlatformHandle()?.Handle;

                if (handle == null) return;

                var systemFileDialog = App.Current.Container.Resolve<ISystemFileDialog>();
                filePath = await systemFileDialog.SaveFileAsync(handle.Value, fileMess.FileName, "保存文件",
                    "", [$"{fileMess.FileType}", $"*{fileMess.FileType}"]);
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
                    Message = "文件另存成功",
                    Type = NotificationType.Success
                });
            }
            else
            {
                _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "文件另存失败",
                    Type = NotificationType.Error
                });
            }
        }
        else if (mess is ImageMessDto imageMess && imageMess.ImageSource != null)
        {
            // 获取文件地址
            string filePath = "";
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow;
                var handle = window!.TryGetPlatformHandle()?.Handle;

                if (handle == null) return;

                var systemFileDialog = App.Current.Container.Resolve<ISystemFileDialog>();
                filePath = await systemFileDialog.SaveFileAsync(handle.Value, imageMess.FilePath, "保存图片",
                    "", [$".png", $"*.png"]);
            }

            if (string.IsNullOrWhiteSpace(filePath)) return;

            using (var fileStream = new FileStream(filePath, FileMode.Create))
                imageMess.ImageSource.Save(fileStream);

            _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "图片另存成功",
                Type = NotificationType.Success
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

        Dispatcher.UIThread.Post(() =>
        {
            // 初始化侧边栏
            RegionManager.RequestNavigate(RegionNames.ChatSideRegion, nameof(FriendSideView), new NavigationParameters
            {
                { "SelectedFriend", SelectedFriend?.FriendRelatoinDto }
            });
        });
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

        RegionManager?.Regions[RegionNames.ChatSideRegion].RemoveAll();
    }

    #endregion
}