using DotNetty.Transport.Channels;

namespace SocketClient.Client
{
    public class SocketClientBuilder
    {
        List<Type> types = new List<Type>();

        public void AddHandler<T>() where T : IChannelHandler
        {
            types.Add(typeof(T));
        }

        public List<Type> GetChannels()
            => types;
    }
}