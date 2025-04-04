using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class UpdateUserDataResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdateUserDataResponse>(container);