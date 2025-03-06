using Google.Protobuf;

namespace ChatClient.MessageOperate
{
    public interface IProcessor<in T> where T : IMessage
    {
        Task Process(T message);
    }
}
