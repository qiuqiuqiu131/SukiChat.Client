using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class CallResponseProcessor(IContainerProvider container)
    : ProcessorBase<CallResponse>(container);