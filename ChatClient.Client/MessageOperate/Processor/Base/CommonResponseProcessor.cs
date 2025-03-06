using ChatServer.Common.Protobuf;
using Google.Protobuf;
using Prism.Ioc;

namespace ChatClient.MessageOperate.Processor.Base;

public class CommonResponseProcessor(IContainerProvider container) 
    : ProcessorBase<CommonResponse>(container)
{
}