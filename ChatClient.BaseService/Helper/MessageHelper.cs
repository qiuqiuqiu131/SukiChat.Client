using Avalonia.Controls.Notifications;
using ChatClient.Client;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;

namespace ChatClient.BaseService.Helper;

internal class MessageHelper : IMessageHelper
{
    private readonly ISocketClient client;
    private readonly IEventAggregator eventAggregator;
    private readonly IConfigurationRoot configuration;

    public MessageHelper(ISocketClient client, IEventAggregator eventAggregator, IConfigurationRoot configuration)
    {
        this.client = client;
        this.eventAggregator = eventAggregator;
        this.configuration = configuration;
    }


    public async Task<bool> SendMessage(IMessage message)
    {
        if (!client.IsConnected || client.Channel == null)
        {
            // eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            // {
            //     Message = "未连接到服务器，请检查网络连接或服务器状态。",
            //     Type = NotificationType.Error
            // });
            return false;
        }

        try
        {
            await client.Channel.WriteAndFlushProtobufAsync(message);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<T?> SendMessageWithResponse<T>(IMessage message) where T : IMessage
    {
        DateTime startTime = DateTime.Now;

        TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
        var token = eventAggregator.GetEvent<ResponseEvent<T>>().Subscribe(e =>
        {
            if (!taskCompletionSource.Task.IsCompleted)
                taskCompletionSource.SetResult(e);
        });

        if (!client.IsConnected || client.Channel == null)
            return default;

        try
        {
            // 发送消息
            await client.Channel.WriteAndFlushProtobufAsync(message);

            Task wait = Task.Delay(TimeSpan.FromSeconds(int.Parse(configuration["OutOfTime"]!)));
            var task = await Task.WhenAny(taskCompletionSource.Task, wait);
            token?.Dispose();


            if (task == taskCompletionSource.Task)
                return taskCompletionSource.Task.Result;
            else
            {
                taskCompletionSource.SetCanceled();
                eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "消息处理超时，请检查网络连接或服务器状态。",
                    Type = NotificationType.Error
                });
                return default;
            }
        }
        catch
        {
            token?.Dispose();
            return default;
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            Console.WriteLine(
                $"Message Sent: {message.GetType().Name}, Time Taken: {(endTime - startTime).TotalMilliseconds} ms");
        }
    }

    public async Task<T?> SendMessageWithResponse<T>(IMessage message, TimeSpan MaxTime,
        CancellationTokenSource? cancellationTokenSource = null) where T : IMessage
    {
        TaskCompletionSource<T> taskCompletionSource =
            new TaskCompletionSource<T>(cancellationTokenSource?.Token ?? CancellationToken.None);
        var token = eventAggregator.GetEvent<ResponseEvent<T>>().Subscribe(e =>
        {
            if (!taskCompletionSource.Task.IsCompleted)
                taskCompletionSource.SetResult(e);
        });

        if (!client.IsConnected || client.Channel == null)
        {
            // eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            // {
            //     Message = "未连接到服务器，请检查网络连接或服务器状态。",
            //     Type = NotificationType.Error
            // });
            return default;
        }

        try
        {
            // 发送消息
            await client.Channel.WriteAndFlushProtobufAsync(message);

            Task wait = Task.Delay(MaxTime);
            var task = await Task.WhenAny(taskCompletionSource.Task, wait);
            token?.Dispose();


            if (task == taskCompletionSource.Task)
            {
                return taskCompletionSource.Task.Result;
            }
            else
            {
                taskCompletionSource.SetCanceled();
                eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "消息处理超时，请检查网络连接或服务器状态。",
                    Type = NotificationType.Error
                });
                return default;
            }
        }
        catch
        {
            token?.Dispose();
            return default;
        }
    }
}