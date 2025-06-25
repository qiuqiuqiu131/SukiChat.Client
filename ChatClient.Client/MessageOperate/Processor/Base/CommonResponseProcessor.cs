using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Base;

public class CommonResponseProcessor(IContainerProvider container)
    : ProcessorBase<CommonResponse>(container)
{
}