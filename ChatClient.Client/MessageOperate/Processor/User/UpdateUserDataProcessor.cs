using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class UpdateUserDataProcessor(IContainerProvider container)
    : ProcessorBase<UpdateUserData>(container);