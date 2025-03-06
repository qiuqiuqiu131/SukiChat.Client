using System.Reflection;
using ChatClient.IOServer.Handler;
using ChatClient.MessageOperate;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace ChatClient.Client;

public static class ClientExtenstions
{
    /// <summary>
    /// 将实现了 IProcessor<> 接口的类注册到依赖注入容器中
    /// </summary>
    /// <param name="services"></param>
    internal static void AddProcessors(this IContainerRegistry containerRegistry)
    {
        // 获取基类 ProcessorBase<> 的类型
        var processorBaseType = typeof(ProcessorBase<>);
        // 获取当前执行程序集中的所有类型，并筛选出继承了 ProcessorBase<> 基类的类
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == processorBaseType
                && t.IsClass && !t.IsAbstract).ToList();

        // 遍历筛选出的类型
        foreach (var type in types)
        {
            // 获取该类型的基类 ProcessorBase<> 的泛型参数
            var genericArgument = type.BaseType.GetGenericArguments().First();
            // 构建 IProcessor<> 接口类型
            var interfaceType = typeof(IProcessor<>).MakeGenericType(genericArgument);
            // 将接口和实现类注册到依赖注入容器中
            containerRegistry.RegisterScoped(interfaceType, type);
        }
    }

    /// <summary>
    /// 配置客户端Handler处理器
    /// </summary>
    /// <param name="builder"></param>
    internal static SocketClientBuilder HandlerInit(this SocketClientBuilder builder)
    {
        builder.AddHandler<ClientConnectHandler>();
        builder.AddHandler<EchoClientHandler>();
        return builder;
    }
}