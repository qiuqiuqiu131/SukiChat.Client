using DotNetty.Transport.Channels;

namespace ChatClient.Resources.Clients;

public interface IFileClient
{
    IChannel? ReturnChannel();
    void Clear();
}