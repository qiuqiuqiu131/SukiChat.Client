using System.Collections.Concurrent;
using System.Reflection;
using ChatServer.Common.Helper;
using Google.Protobuf;

namespace SocketClient.MessageOperate
{
    public interface IProtobufDispatcher
    {
        void SendMessage(byte[] data);
    }

    public interface IServer
    {
        void Start();
    }

    /// <summary>
    /// Protobuf消息分发器
    /// 用于将输入的Byte[]数据流转成IMessage
    /// </summary>
    public class ProtobufDispatcher : IProtobufDispatcher, IServer
    {
        // 服务
        private readonly IContainerProvider container;

        // 消息队列
        private readonly BlockingCollection<IMessage> queue = new();

        // 信号量,控制消息队列大小
        private readonly SemaphoreSlim semaphore;

        public ProtobufDispatcher(IContainerProvider container)
        {
            this.container = container;

            semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 解析字节流，转成IMessage，进入消息队列
        /// </summary>
        /// <param name="channel">连接，消息来源</param>
        /// <param name="data">字节流</param>
        public void SendMessage(byte[] data)
        {
            // 解析IMessage
            IMessage message = ProtobufHelper.ParseFrom(data);

            // 分发消息
            queue.Add(message);
        }

        /// <summary>
        /// 业务服务器启动
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Start()
        {
            // 启动消费者任务
            Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);
        }

        #region Process

        /// <summary>
        /// 控制消息队列的执行
        /// </summary>
        private void ProcessQueue()
        {
            foreach (var unit in queue.GetConsumingEnumerable())
            {
                semaphore.Wait();
                Task.Run(async () =>
                {
                    try
                    {
                        await OperateMessageUnit(unit);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }
        }

        /// <summary>
        /// 处理消息单元MessageUnit<T> ,将其分发给对应的处理器processor
        /// </summary>
        /// <param name="obj"></param>
        private async Task OperateMessageUnit(IMessage obj)
        {
            // 获取消息处理器，根据IMessage类型获取对应的处理器
            Type type = obj.GetType();
            Type? processorType = typeof(IProcessor<>).MakeGenericType(type);
            Type? processorsType = typeof(IEnumerable<>).MakeGenericType(processorType);

            // 创建一个作用域，生成处理消息的具体业务逻辑单元
            using (var scope = container.CreateScope())
            {
                // 获取处理器实例，一个Protobuf消息对应多个处理器
                var processors = (IEnumerable<object>)scope.Resolve(processorsType)!;

                // 获取处理方法
                MethodInfo? processMethod = processorType.GetMethod("Process");

                // 触发处理方法
                foreach (var processor in processors)
                {
                    // 调用处理器
                    if (processMethod != null)
                    {
                        var task = (Task)processMethod.Invoke(processor, [obj])!;
                        await task;
                    }
                }
            }
        }

        #endregion
    }
}