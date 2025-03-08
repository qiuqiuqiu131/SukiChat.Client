using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;

public class ChatGroupPanelViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    public ChatInputPanelViewModel ChatInputPanelViewModel { get; init; }

    private GroupChatDto? selectedGroup;

    public GroupChatDto? SelectedGroup
    {
        get => selectedGroup;
        set => SetProperty(ref selectedGroup, value);
    }

    public UserDto User => _userManager.User!;

    #region Command

    public DelegateCommand SearchMoreCommand { get; private set; }
    public DelegateCommand<GroupChatData> HeadClickCommand { get; private set; }
    public DelegateCommand<FileMessDto> FileMessageClickCommand { get; private set; }

    #endregion

    public ChatGroupPanelViewModel(IContainerProvider containerProvider, IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        ChatInputPanelViewModel = new ChatInputPanelViewModel(SendChatMessage, SendChatMessages);

        HeadClickCommand = new DelegateCommand<GroupChatData>(HeadClick);
        SearchMoreCommand = new DelegateCommand(SearchMoreGroupChatMessage);
        FileMessageClickCommand = new DelegateCommand<FileMessDto>(FileDownload);
    }

    private void HeadClick(GroupChatData obj)
    {
        //TODO: 转跳到群组详情页
        Console.WriteLine("HeadClick:" + obj);
    }

    /// <summary>
    /// 加载更多历史记录
    /// </summary>
    public async void SearchMoreGroupChatMessage()
    {
        if (SelectedGroup == null) return;

        var chatMessages = SelectedGroup.ChatMessages;

        var chatPackService = _containerProvider.Resolve<IGroupChatPackService>();
        var chatDatas =
            await chatPackService.GetGroupChatDataAsync(_userManager.User?.Id, SelectedGroup.GroupId,
                SelectedGroup.ChatMessages[0].ChatId,
                15);

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
        if (chatDatas.Count() != 15)
            SelectedGroup.HasMoreMessage = false;
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
        var path = Path.Combine("Groups", SelectedGroup!.GroupId, "ChatFile");
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
        if (SelectedGroup == null) return;

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

        await SendChatMessagesInternal(chatMessageDtos);
    }

    /// <summary>
    /// 处理单条聊天消息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="mess"></param>
    public async void SendChatMessage(ChatMessage.ContentOneofCase type, object mess)
    {
        if (SelectedGroup == null) return;

        var chatMessage = new ChatMessageDto { Type = type, Content = mess };
        await SendChatMessagesInternal(new List<ChatMessageDto> { chatMessage });
    }

    private async Task SendChatMessagesInternal(List<ChatMessageDto> chatMessageDtos)
    {
        if (SelectedGroup == null) return;

        bool showTime = SelectedGroup.ChatMessages.Count == 0 ||
                        DateTime.Now - SelectedGroup.ChatMessages.Last().Time > TimeSpan.FromMinutes(5);

        var chatData = new GroupChatData
        {
            ShowTime = showTime,
            IsUser = true,
            IsWriting = true,
            Time = DateTime.Now,
            ChatMessages = chatMessageDtos
        };

        SelectedGroup.ChatMessages.Add(chatData);

        var chatService = _containerProvider.Resolve<IChatService>();
        var (state, _) =
            await chatService.SendGroupChatMessage(_userManager.User!.Id, SelectedGroup.GroupId, chatMessageDtos);

        chatData.IsWriting = false;
        chatData.IsError = !state;
        chatData.Time = DateTime.Now;

        SelectedGroup.UpdateChatMessages();
    }

    #endregion

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedGroup = navigationContext.Parameters.GetValue<GroupChatDto>("SelectedGroup");
        ChatInputPanelViewModel.UpdateChatMessages(SelectedGroup.InputMessages);
    }
}