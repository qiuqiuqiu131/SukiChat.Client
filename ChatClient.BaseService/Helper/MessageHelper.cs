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
            return false;

        await client.Channel.WriteAndFlushProtobufAsync(message);
        return true;
    }

    public async Task<T?> SendMessageWithResponse<T>(IMessage message) where T : IMessage
    {
        TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
        var token = eventAggregator.GetEvent<ResponseEvent<T>>().Subscribe(e =>
        {
            if (!taskCompletionSource.Task.IsCompleted)
                taskCompletionSource.SetResult(e);
        });

        if (!client.IsConnected || client.Channel == null)
            return default(T);

        // 发送消息
        await client.Channel.WriteAndFlushProtobufAsync(message);

        Task wait = Task.Delay(TimeSpan.FromSeconds(int.Parse(configuration["OutOfTime"]!)));
        var task = await Task.WhenAny(taskCompletionSource.Task, wait);
        token?.Dispose();


        if (task == taskCompletionSource.Task)
        {
            return taskCompletionSource.Task.Result;
        }
        else
        {
            taskCompletionSource.SetCanceled();
            return default(T);
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
            return default(T);

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
            return default(T);
        }
    }
}