using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupRequestFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestFromServer>(container);