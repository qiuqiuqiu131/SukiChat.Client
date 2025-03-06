using System.Diagnostics;
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class FriendRequestProcessor(IContainerProvider container) : ProcessorBase<FriendRequestFromServer>(container);