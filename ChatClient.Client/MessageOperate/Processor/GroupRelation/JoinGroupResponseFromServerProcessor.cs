using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseFromServer>(container);