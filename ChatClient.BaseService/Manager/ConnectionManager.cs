using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

internal class ConnectionManager : BindableBase, IConnection
{
    public Connect IsConnected { get; private set; }

    public ConnectionManager(IEventAggregator eventAggregator)
    {
        IsConnected = new Connect();

        eventAggregator.GetEvent<ConnectEvent>().Subscribe(c => { IsConnected.IsConnected = c; });
    }
}