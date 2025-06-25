using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatClient.Avalonia.Common;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Desktop.Views.ChatPages.ChatViews.SideRegion;
using ChatClient.Tool.Config;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
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
public class ChatGroupPanelViewModel : ViewModelBase, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly AppSettings _appSettings;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IUserManager _userManager;

    public IRegionManager RegionManager { get; set; }

    public ThemeStyle ThemeStyle { get; set; }

    public ChatInputPanelViewModel ChatInputPanelViewModel { get; private set; }

    private GroupChatDto? selectedGroup;

    public GroupChatDto? SelectedGroup
    {
        get => selectedGroup;
        set => SetProperty(ref selectedGroup, value);
    }

    private bool sendMessageButtonVisible = true;

    public bool SendMessageButtonVisible
    {
        get => sendMessageButtonVisible;
        set => SetProperty(ref sendMessageButtonVisible, value);
    }

    public UserDto User => _userManager.User?.UserDto!;

    #region Command

    public DelegateCommand SearchMoreCommand { get; private set; }
    public DelegateCommand<GroupChatData> RetractMessageCommand { get; private set; }
    public DelegateCommand<object> ShareMessageCommand { get; private set; }
    public DelegateCommand<GroupChatData> DeleteMessageCommand { get; private set; }
    public AsyncDelegateCommand<FileMessDto> FileMessageClickCommand { get; private set; }
    public AsyncDelegateCommand<object> FileRestoreCommand { get; private set; }

    #endregion

    public ChatGroupPanelViewModel(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        IRegionManager regionManager,
        AppSettings appSettings,
        ISukiDialogManager sukiDialogManager,
        IUserManager userManager,
        IThemeStyle themeStyle)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _appSettings = appSettings;
        _sukiDialogManager = sukiDialogManager;
        _userManager = userManager;
        ThemeStyle = themeStyle.CurrentThemeStyle;

        RegionManager = regionManager.CreateRegionManager();

        ChatInputPanelViewModel =
            new ChatInputPanelViewModel(SendChatMessage, SendChatMessages, null, d => SendMessageButtonVisible = d);

        SearchMoreCommand = new DelegateCommand(SearchMoreGroupChatMessage);
        RetractMessageCommand = new DelegateCommand<GroupChatData>(RetractMessage);
        DeleteMessageCommand = new DelegateCommand<GroupChatData>(DeleteMessage);
        ShareMessageCommand = new DelegateCommand<object>(ShareMessage);
        FileRestoreCommand = new AsyncDelegateCommand<object>(FileRestoreDownload);
        FileMessageClickCommand = new AsyncDelegateCommand<FileMessDto>(FileDownload);
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
    /// 撤回聊天记录
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void RetractMessage(GroupChatData obj)
    {
        if (SelectedGroup == null) return;

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
                var response = await chatService.RetractChatMessage(_userManager.User.Id, obj.ChatId, FileTarget.Group);

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
    private void DeleteMessage(GroupChatData obj)
    {
        if (SelectedGroup == null) return;

        async void DeleteMessageCallback(IDialogResult dialogResult)
        {
            if (dialogResult.Result != ButtonResult.OK) return;
            var index = SelectedGroup.ChatMessages.IndexOf(obj);
            SelectedGroup.ChatMessages.Remove(obj);
            if (SelectedGroup.ChatMessages.Count > index && index > 0)
            {
                var last = SelectedGroup.ChatMessages[index];
                if (last.Time - SelectedGroup.ChatMessages[index - 1].Time > TimeSpan.FromMinutes(3))
                    last.ShowTime = true;
                else
                    last.ShowTime = false;
            }
            else if (SelectedGroup.ChatMessages.Count > index)
                SelectedGroup.ChatMessages[0].ShowTime = true;

            var chatService = _containerProvider.Resolve<IChatService>();
            await chatService.DeleteChatMessage(_userManager.User!.Id, obj.ChatId, FileTarget.Group);

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
    public async void SearchMoreGroupChatMessage()
    {
        if (SelectedGroup == null) return;

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        using var scopeProvider = _containerProvider.CreateScope();
        var groupPackService = scopeProvider.Resolve<IGroupChatPackService>();

        int nextCount = _appSettings.ChatMessageCount;
        var chatDatas =
            await groupPackService.GetGroupChatDataAsync(User?.Id, SelectedGroup.GroupId,
                SelectedGroup.ChatMessages[0].ChatId,
                nextCount);

        if (chatDatas.Count != nextCount)
            SelectedGroup.HasMoreMessage = false;
        else
            SelectedGroup.HasMoreMessage = true;

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

            SelectedGroup.ChatMessages.Insert(0, chatData);
            var duration = SelectedGroup.ChatMessages[1].Time - chatData.Time;
            if (duration > TimeSpan.FromMinutes(3))
                SelectedGroup.ChatMessages[1].ShowTime = true;
            else
                SelectedGroup.ChatMessages[1].ShowTime = false;
        }

        // 将最后一条消息的时间显示出来
        if (SelectedGroup.ChatMessages.Count > 0)
        {
            SelectedGroup.ChatMessages[0].ShowTime = true;

            SelectedGroup.UnReadMessageCount = 0;

            var maxChatId = SelectedGroup.ChatMessages.Max(d => d.ChatId);

            var chatService = _containerProvider.Resolve<IChatService>();
            _ = chatService.ReadAllChatMessage(User!.Id, SelectedGroup.GroupRelationDto!.Id, maxChatId,
                FileTarget.Group);
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
            .GetFileInfo(Path.Combine("Groups", SelectedGroup.GroupId,
                "ChatFile", fileMess.FileName)).FullName;

        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 记录另存文件路径
        fileMess.TargetFilePath = filePath;

        fileMess.IsDownloading = true;
        var progress = new Progress<double>(d => fileMess.DownloadProgress = d);

        // 开始下载文件
        var fileIOHelper = _containerProvider.Resolve<IFileIOHelper>();
        var path = Path.Combine("Groups", SelectedGroup!.GroupId, "ChatFile");
        var result =
            await fileIOHelper.DownloadLargeFileAsync(path, fileMess.FileName, filePath, progress);

        // 记录文件下载完毕
        fileMess.IsDownloading = false;
        fileMess.IsSuccess = result;
        fileMess.IsExit = result;

        var chatService = _containerProvider.Resolve<IChatService>();
        await chatService.UpdateFileMess(_userManager.User.Id, fileMess, FileTarget.Group);

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
                await fileOperateHelper.SaveAsFile(SelectedGroup.GroupId,
                    "ChatFile", fileMess.FileName, filePath,
                    FileTarget.Group);

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
        if (SelectedGroup == null) return (false, FileTarget.Group);

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

        return (await SendChatMessagesInternal(chatMessageDtos), FileTarget.Group);
    }

    /// <summary>
    /// 处理单条聊天消息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="mess"></param>
    public async Task<(bool, FileTarget)> SendChatMessage(ChatMessage.ContentOneofCase type, object mess)
    {
        if (SelectedGroup == null) return (false, FileTarget.Group);

        var chatMessage = new ChatMessageDto { Type = type, Content = mess };
        return (await SendChatMessagesInternal([chatMessage]), FileTarget.Group);
    }

    private async Task<bool> SendChatMessagesInternal(List<ChatMessageDto> chatMessageDtos)
    {
        if (SelectedGroup == null) return false;

        bool showTime = SelectedGroup.ChatMessages.Count == 0 ||
                        DateTime.Now - SelectedGroup.ChatMessages.Last().Time > TimeSpan.FromMinutes(5);

        var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();

        var chatData = new GroupChatData
        {
            ShowTime = showTime,
            IsUser = true,
            IsWriting = true,
            Time = DateTime.Now,
            ChatMessages = chatMessageDtos,
            Owner = await userDtoManager.GetGroupMemberDto(selectedGroup.GroupId, _userManager.User!.Id)
        };

        SelectedGroup.ChatMessages.Add(chatData);

        var chatService = _containerProvider.Resolve<IChatService>();
        var (state, chatId) =
            await chatService.SendGroupChatMessage(_userManager.User!.Id, SelectedGroup.GroupId, chatMessageDtos);

        chatData.IsWriting = false;
        chatData.IsError = !state;
        chatData.Time = DateTime.Now;
        chatData.ChatId = int.TryParse(chatId, out int id) ? id : 0;

        SelectedGroup.UpdateChatMessages();

        return state;
    }

    #endregion

    #region Region

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedGroup = navigationContext.Parameters.GetValue<GroupChatDto>("SelectedGroup");
        ChatInputPanelViewModel.UpdateChatMessages(SelectedGroup.InputMessages);

        if (SelectedGroup.GroupRelationDto != null)
        {
            if (SelectedGroup.GroupRelationDto.GroupDto != null)
                SelectedGroup.GroupRelationDto!.GroupDto.OnGroupChanged += GroupDtoOnOnGroupChanged;
        }

        Dispatcher.UIThread.Post(() =>
        {
            RegionManager.RequestNavigate(RegionNames.ChatSideRegion,
                nameof(GroupSideView), new NavigationParameters
                {
                    { "SelectedGroup", SelectedGroup.GroupRelationDto }
                });
        });
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (SelectedGroup is { GroupRelationDto: not null })
        {
            if (SelectedGroup.GroupRelationDto.GroupDto != null)
                SelectedGroup.GroupRelationDto!.GroupDto.OnGroupChanged -= GroupDtoOnOnGroupChanged;
            SelectedGroup = null;
        }
    }

    private async void GroupDtoOnOnGroupChanged()
    {
        if (SelectedGroup?.GroupRelationDto?.GroupDto == null) return;

        var groupService = _containerProvider.Resolve<IGroupService>();
        await groupService.UpdateGroup(_userManager.User!.Id, SelectedGroup.GroupRelationDto.GroupDto);
    }

    public void Destroy()
    {
        SelectedGroup = null;
        ChatInputPanelViewModel?.Dispose();

        RegionManager?.Regions[RegionNames.ChatSideRegion].RemoveAll();
    }

    #endregion
}