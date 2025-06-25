using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Relation;

public class NewMemberJoinMessageProcessor(IContainerProvider container)
    : ProcessorBase<NewMemberJoinMessage>(container);