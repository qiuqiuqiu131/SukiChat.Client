using ChatClient.Tool.Data;

namespace ChatClient.Tool.ManagerInterface;

public interface IConnection
{
    public Connect IsConnected { get; }
}