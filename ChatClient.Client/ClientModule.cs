using ChatClient.IOServer.Handler;
using ChatClient.MessageOperate;
using Prism.Ioc;
using Prism.Modularity;

namespace ChatClient.Client;

public class ClientModule:IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册客户端
        containerRegistry.RegisterSingleton<ISocketClient, SocketClient>();

        // 注册客户端处理器
        containerRegistry.Register<EchoClientHandler>();
        containerRegistry.Register<ClientConnectHandler>();

        // 注册消息分发器
        containerRegistry.RegisterSingleton<ProtobufDispatcher>();
        containerRegistry.RegisterSingleton<IProtobufDispatcher>(d => d.Resolve<ProtobufDispatcher>());
        containerRegistry.RegisterSingleton<IServer>(d => d.Resolve<ProtobufDispatcher>());

        // 注册消息处理器
        containerRegistry.AddProcessors();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var server = containerProvider.Resolve<IServer>();
        server.Start();

        // 启动客户端
        var socketClient = containerProvider.Resolve<ISocketClient>();
        socketClient.Start();
    }
}