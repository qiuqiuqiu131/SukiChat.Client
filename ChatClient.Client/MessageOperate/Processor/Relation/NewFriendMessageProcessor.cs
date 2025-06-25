using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Relation;

public class NewFriendMessageProcessor(IContainerProvider container) : ProcessorBase<NewFriendMessage>(container);