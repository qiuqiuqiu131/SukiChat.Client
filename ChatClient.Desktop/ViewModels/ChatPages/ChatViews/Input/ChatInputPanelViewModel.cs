using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Media.AudioPlayer;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Media.Audio;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;

public class ChatInputPanelViewModel : ViewModelBase, IDisposable
{
    private AvaloniaList<object>? _inputMessages;

    public AvaloniaList<object>? InputMessages
    {
        get => _inputMessages;
        set => SetProperty(ref _inputMessages, value);
    }

    private bool audioRecorderIsOpen;

    public bool AudioRecorderIsOpen
    {
        get => audioRecorderIsOpen;
        set => SetProperty(ref audioRecorderIsOpen, value);
    }

    private AudioRecorderViewModel audioRecorderViewModel;

    public AudioRecorderViewModel AudioRecorderViewModel
    {
        get => audioRecorderViewModel;
        private set => SetProperty(ref audioRecorderViewModel, value);
    }

    public AsyncDelegateCommand SendMessageCommand { get; init; }
    public DelegateCommand SelectFileAndSendCommand { get; init; }
    public DelegateCommand<string> SendFileWithPathCommand { get; init; }
    public DelegateCommand ScreenShotCommand { get; init; }
    public DelegateCommand ClearInputMessages { get; init; }
    public DelegateCommand SendVoiceMessageCommand { get; init; }

    private Func<ChatMessage.ContentOneofCase, object, Task<(bool, FileTarget)>> sendChatMessage;
    private Func<IEnumerable<object>, Task<(bool, FileTarget)>> sendChatMessages;
    private Action<bool>? inputMessageChanged;
    private Action<bool> sendChatMessageVisible;

    private bool isWriting;

    public ChatInputPanelViewModel(
        Func<ChatMessage.ContentOneofCase, object, Task<(bool, FileTarget)>> SendChatMessage,
        Func<IEnumerable<object>, Task<(bool, FileTarget)>> SendChatMessages,
        Action<bool>? InputMessageChanged = null,
        Action<bool> SendChatMessageVisible = null)
    {
        sendChatMessages = SendChatMessages;
        sendChatMessage = SendChatMessage;
        inputMessageChanged = InputMessageChanged;
        sendChatMessageVisible = SendChatMessageVisible;

        AudioRecorderViewModel = new AudioRecorderViewModel(this);

        SendMessageCommand = new AsyncDelegateCommand(SendMessages, CanSendMessages);
        SelectFileAndSendCommand = new DelegateCommand(SelectFileAndSend);
        SendFileWithPathCommand = new DelegateCommand<string>(SendFileWithPath);
        ScreenShotCommand = new DelegateCommand(ScreenShot);
        ClearInputMessages = new DelegateCommand(ClearInput);
        SendVoiceMessageCommand = new DelegateCommand(SendVoiceMessage);
    }

    // 发送语音消息
    private void SendVoiceMessage()
    {
        AudioRecorderIsOpen = true;
        sendChatMessageVisible?.Invoke(false);
    }

    public void ReturnFromAudioRecorder()
    {
        AudioRecorderIsOpen = false;
        sendChatMessageVisible?.Invoke(true);
    }

    public void UpdateChatMessages(AvaloniaList<object> chatList)
    {
        if (InputMessages != null)
            InputMessages.CollectionChanged -= NotifyInputMessagesChanged;
        InputMessages = chatList;
        InputMessages.CollectionChanged += NotifyInputMessagesChanged;

        if (InputMessages.Count == 1 && InputMessages[0] is string str && string.IsNullOrEmpty(str))
            isWriting = false;
        else
            isWriting = true;
    }

    private void ClearInput()
    {
        InputMessages?.Clear();
    }

    /// <summary>
    /// 屏幕截图，返回截图的Bitmap并将Bitmap放入输入框中
    /// </summary>
    private async void ScreenShot()
    {
        try
        {
            var bitmap = await ScreenShotHelper.ScreenShot();
            if (InputMessages.Last() is string str && string.IsNullOrEmpty(str))
            {
                var index = InputMessages.Count - 1;
                InputMessages?.Add(bitmap);
                InputMessages?.RemoveAt(index);
            }
            else
                InputMessages?.Add(bitmap);
        }
        catch (Exception e)
        {
            // 取消截图
        }
    }

    private void NotifyInputMessagesChanged(object? sender, NotifyCollectionChangedEventArgs? e)
    {
        SendMessageCommand.RaiseCanExecuteChanged();

        if (InputMessages == null) return;

        if (isWriting && InputMessages.Count == 1 && InputMessages[0] is string str1 && string.IsNullOrEmpty(str1))
        {
            isWriting = false;
            inputMessageChanged?.Invoke(false);
        }
        else if (!isWriting && (InputMessages.Count > 1 || InputMessages.Count == 1 &&
                     (InputMessages[0] is not string str2 ||
                      !string.IsNullOrEmpty(str2))))
        {
            isWriting = true;
            inputMessageChanged?.Invoke(true);
        }
    }

    private bool CanSendMessages()
    {
        if (InputMessages == null
            || InputMessages.Count == 0
            || InputMessages.Count == 1 && InputMessages[0] is string str && string.IsNullOrWhiteSpace(str))
            return false;
        return true;
    }

    #region SendMessages

    public async Task SendVoiceMessage(Stream voiceData)
    {
        if (voiceData.Length > 5 * 1024 * 1024)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "发送语音不能超过5MB",
                Type = NotificationType.Warning
            });
            return;
        }

        try
        {
            IPlatformAudioPlayer? audioPlayer = AudioPlayerFactory.CreateAudioPlayer();
            audioPlayer.LoadFromMemory(voiceData);
            var voiceMess = new VoiceMessDto
            {
                FilePath = "",
                AudioData = voiceData,
                FileSize = voiceData.Length,
                Duration = audioPlayer.TotalTime
            };
            audioPlayer.Dispose();

            await sendChatMessage(ChatMessage.ContentOneofCase.VoiceMess, voiceMess);
        }
        catch (PlatformNotSupportedException)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "当前平台不支持音频文件",
                Type = NotificationType.Warning
            });
        }
    }

    // 选择图片并发送
    private void SendImageMessage(string filePath)
    {
        Bitmap? bitmap;
        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            bitmap = new Bitmap(stream);

        var imageMess = new ImageMessDto
        {
            FilePath = filePath,
            FileSize = (int)new FileInfo(filePath).Length,
            ImageSource = bitmap
        };

        var fileMess = new FileMessDto
        {
            FileName = filePath,
            FileSize = (int)new FileInfo(filePath).Length,
            FileType = new FileInfo(filePath).Extension,
            TargetFilePath = filePath
        };

        var sukiDialog = App.Current.Container.Resolve<ISukiDialogManager>();
        sukiDialog.CreateDialog()
            .WithViewModel(d => new SendFileDialogViewModel(d,
                new DialogParameters { { "Mess", new List<object> { fileMess } } }, d =>
                {
                    if (d.Result != ButtonResult.OK) return;
                    sendChatMessage(ChatMessage.ContentOneofCase.ImageMess, imageMess);
                })).TryShow();
    }

    // 选择文件并发送
    private void SendFileMessage(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        var fileMess = new FileMessDto
        {
            FileName = filePath,
            FileSize = (int)fileInfo.Length,
            FileType = fileInfo.Extension,
            TargetFilePath = filePath,
            IsSuccess = true,
            IsUser = true
        };

        var sukiDialog = App.Current.Container.Resolve<ISukiDialogManager>();
        sukiDialog.CreateDialog()
            .WithViewModel(d => new SendFileDialogViewModel(d,
                new DialogParameters { { "Mess", new List<object> { fileMess } } },
                async d =>
                {
                    if (d.Result != ButtonResult.OK) return;

                    var (result, target) = await sendChatMessage(ChatMessage.ContentOneofCase.FileMess, fileMess);
                    if (result)
                    {
                        fileMess.IsExit = true;
                        var chatService = App.Current.Container.Resolve<IChatService>();
                        var _userManager = App.Current.Container.Resolve<IUserManager>();
                        await chatService.UpdateFileMess(_userManager.User.Id, fileMess, target);
                    }
                })).TryShow();
    }

    /// <summary>
    /// 选择文件并发送(单条消息)
    /// </summary>
    private async void SelectFileAndSend()
    {
        // 获取文件地址
        string filePath = "";
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            var handle = window!.TryGetPlatformHandle()?.Handle;

            if (handle == null) return;

            var systemFileDialog = App.Current.Container.Resolve<ISystemFileDialog>();
            filePath = await systemFileDialog.OpenFileAsync(handle.Value, "选择文件");
        }

        SendFileWithPath(filePath);
    }

    private void SendFileWithPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > 5 * 1024 * 1024)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "发送文件不能超过5MB",
                Type = NotificationType.Warning
            });
            return;
        }

        if (FileExtensionTool.IsImage(filePath))
            SendImageMessage(filePath);
        else
            SendFileMessage(filePath);
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    private async Task SendMessages()
    {
        if (InputMessages.Last() is string str && string.IsNullOrEmpty(str))
            InputMessages.RemoveAt(InputMessages.Count - 1);

        List<object> messages = InputMessages.Select(d => d).ToList();
        InputMessages.RemoveRange(0, InputMessages.Count);

        await sendChatMessages.Invoke(messages);
    }

    #endregion

    public void Dispose()
    {
        sendChatMessages = null;
        sendChatMessage = null;
        inputMessageChanged = null;
        AudioRecorderViewModel?.Dispose();

        InputMessages = null;
    }
}