using Google.Protobuf;

namespace SocketClient.MessageOperate
{
    public interface IProcessor<in T> where T : IMessage
    {
        Task Process(T message);
    }
}