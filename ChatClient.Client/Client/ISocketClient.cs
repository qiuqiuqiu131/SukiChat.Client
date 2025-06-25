using DotNetty.Transport.Channels;

namespace SocketClient.Client
{
    public interface ISocketClient
    {
        bool IsConnected { get; }
        IChannel? Channel { get; }

        Task Start();
        Task Stop();

        Task ChangeAddress();
    }
}