using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupResponseResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseResponseFromServer>(container);