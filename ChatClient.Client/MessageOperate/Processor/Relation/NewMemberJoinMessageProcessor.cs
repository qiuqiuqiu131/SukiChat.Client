using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class NewMemberJoinMessageProcessor(IContainerProvider container)
    : ProcessorBase<NewMemberJoinMessage>(container);