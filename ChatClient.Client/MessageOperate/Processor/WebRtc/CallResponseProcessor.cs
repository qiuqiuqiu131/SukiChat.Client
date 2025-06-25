using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class CallResponseProcessor(IContainerProvider container)
    : ProcessorBase<CallResponse>(container);