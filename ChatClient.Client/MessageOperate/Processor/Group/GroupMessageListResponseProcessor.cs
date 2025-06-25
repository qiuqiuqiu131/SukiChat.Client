using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMessageListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupMessageListResponse>(container);