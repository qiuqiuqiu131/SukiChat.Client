using Google.Protobuf;

namespace ChatClient.Tool.HelperInterface;

public interface IMessageHelper
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> SendMessage(IMessage message);

    /// <summary>
    /// 发送消息并获取相应消息
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="T">想要获取的相应消息</typeparam>
    /// <returns></returns>
    Task<T?> SendMessageWithResponse<T>(IMessage message) where T : IMessage;

    Task<T?> SendMessageWithResponse<T>(IMessage message, TimeSpan MaxTime,
        CancellationTokenSource? cancellationTokenSource = null) where T : IMessage;
}