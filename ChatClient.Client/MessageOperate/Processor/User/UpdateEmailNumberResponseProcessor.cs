using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class UpdateEmailNumberResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdateEmailNumberResponse>(container);