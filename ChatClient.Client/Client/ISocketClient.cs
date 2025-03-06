using System.Net;
using DotNetty.Transport.Channels;

namespace ChatClient.Client
{
    public interface ISocketClient
    {
        bool IsConnected { get; }
        IChannel? Channel { get;}
        
        Task Start();
        Task Stop();
    }
}
