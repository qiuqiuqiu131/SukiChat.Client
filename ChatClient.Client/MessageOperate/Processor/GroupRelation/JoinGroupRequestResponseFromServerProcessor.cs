using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupRequestResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestResponseFromServer>(container);