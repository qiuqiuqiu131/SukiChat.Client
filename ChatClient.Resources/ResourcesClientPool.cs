using System.Net;
using System.Collections.Concurrent;
using ChatClient.Resources.Clients;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Pool;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Configuration;
using BindingFlags = System.Reflection.BindingFlags;

namespace ChatClient.Resources;

public interface IResourcesClientPool
{
    Task<T> GetClientAsync<T>() where T : IFileClient;
    void ReturnClient(IFileClient client);
}

public class ResourcesClientPool : IResourcesClientPool
{
    private readonly EndPoint _endPoint;

    private SimpleChannelPool _channelPool;

    // 添加构造函数缓存字典
    private readonly ConcurrentDictionary<Type, System.Reflection.ConstructorInfo> _constructorCache = new();

    public ResourcesClientPool(IConfigurationRoot configurationRoot)
    {
        _endPoint = new IPEndPoint(IPAddress.Parse(configurationRoot["Address:IP"]!),
            int.Parse(configurationRoot["Address:Resources_Port"]!));

        // 设置环境变量,不记录已发字节流
        Environment.SetEnvironmentVariable("io.netty.allocator.numDirectArenas", "0");
        Environment.SetEnvironmentVariable("io.netty.allocator.numHeapArenas", "0");

        MultithreadEventLoopGroup group = new MultithreadEventLoopGroup();

        Bootstrap bootstrap = new Bootstrap();
        bootstrap
            .Group(group)
            .Channel<TcpSocketChannel>()
            .Option(ChannelOption.TcpNodelay, true)
            .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
            .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default);
        bootstrap.RemoteAddress(_endPoint);
        _channelPool = new SimpleChannelPool(bootstrap, new ResourcesClientPoolHandler(_endPoint));
    }

    /// <summary>
    /// 获取连接池资源服务器连接
    /// </summary>
    /// <returns></returns>
    public async Task<T> GetClientAsync<T>() where T : IFileClient
    {
        var d = await _channelPool.AcquireAsync();
        var type = typeof(T);
        // 从缓存中获取构造函数,如果不存在则创建并添加到缓存
        var constructor = _constructorCache.GetOrAdd(type, t =>
        {
            var ctor = t.GetConstructor([typeof(IChannel)]);
            if (ctor == null)
            {
                throw new InvalidOperationException($"No suitable constructor found for type {t.FullName}");
            }

            return ctor;
        });
        var client = (T)constructor.Invoke([d]);
        return client;
    }

    /// <summary>
    /// 返还连接池资源服务器连接
    /// </summary>
    public async void ReturnClient(IFileClient client)
    {
        IChannel? channel = client.ReturnChannel();
        await _channelPool.ReleaseAsync(channel);
    }
}