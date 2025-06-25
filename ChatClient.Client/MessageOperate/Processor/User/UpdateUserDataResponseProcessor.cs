using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class UpdateUserDataResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdateUserDataResponse>(container);