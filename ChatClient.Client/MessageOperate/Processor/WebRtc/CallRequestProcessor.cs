using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class CallRequestProcessor(IContainerProvider container)
    : ProcessorBase<CallRequest>(container);