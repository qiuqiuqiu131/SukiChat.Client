using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Ioc;
using Action = Avalonia.Xaml.Interactivity.Action;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatInputPanelViewModel : ViewModelBase
{
    private AvaloniaList<object>? _inputMessages;

    public AvaloniaList<object>? InputMessages
    {
        get => _inputMessages;
        set => SetProperty(ref _inputMessages, value);
    }

    public DelegateCommand SendMessageCommand { get; init; }
    public DelegateCommand SelectFileAndSendCommand { get; init; }
    public DelegateCommand ScreenShotCommand { get; init; }
    public DelegateCommand ClearInputMessages { get; init; }

    private readonly Action<ChatMessage.ContentOneofCase, object> sendChatMessage;
    private readonly Action<IEnumerable<object>> sendChatMessages;
    private readonly Action<bool>? inputMessageChanged;

    private bool isWriting;

    public ChatInputPanelViewModel(
        Action<ChatMessage.ContentOneofCase, object> SendChatMessage,
        Action<IEnumerable<object>> SendChatMessages,
        Action<bool>? InputMessageChanged = null)
    {
        sendChatMessages = SendChatMessages;
        sendChatMessage = SendChatMessage;
        inputMessageChanged = InputMessageChanged;

        SendMessageCommand = new DelegateCommand(SendMessages, CanSendMessages);
        SelectFileAndSendCommand = new DelegateCommand(SelectFileAndSend);
        ScreenShotCommand = new DelegateCommand(ScreenShot);
        ClearInputMessages = new DelegateCommand(ClearInput);
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
        InputMessages?.RemoveRange(0, InputMessages.Count);
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
            || InputMessages.Count == 1 && InputMessages[0] is string str && string.IsNullOrEmpty(str))
            return false;
        return true;
    }

    #region SendMessages

    /// <summary>
    /// 选择文件并发送(单条消息)
    /// </summary>
    public async void SelectFileAndSend()
    {
        // 选择图片并发送
        async Task SendImageMessage(string filePath)
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

            sendChatMessage(ChatMessage.ContentOneofCase.ImageMess, imageMess);
        }

        // 选择文件并发送
        async Task SendFileMessage(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            var fileMess = new FileMessDto
            {
                FileName = filePath,
                FileSize = (int)fileInfo.Length,
                FileType = fileInfo.Extension,
                FileProcessDto = new FileProcessDto
                {
                    CurrentSize = 0,
                    MaxSize = fileInfo.Length
                }
            };
            sendChatMessage(ChatMessage.ContentOneofCase.FileMess, fileMess);
        }

        // 获取文件地址
        string filePath = "";
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            var handle = window!.TryGetPlatformHandle()?.Handle;

            if (handle == null) return;

            filePath = await SystemFileDialog.OpenFileAsync(handle.Value, "选择文件", "All Files\0*.*\0");
        }

        if (string.IsNullOrWhiteSpace(filePath)) return;

        if (FileExtensionTool.IsImage(filePath))
            await SendImageMessage(filePath);
        else
            await SendFileMessage(filePath);
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async void SendMessages()
    {
        if (InputMessages.Last() is string str && string.IsNullOrEmpty(str))
            InputMessages.RemoveAt(InputMessages.Count - 1);

        List<object> messages = InputMessages.Select(d => d).ToList();
        InputMessages.RemoveRange(0, InputMessages.Count);

        sendChatMessages.Invoke(messages);
    }

    #endregion
}