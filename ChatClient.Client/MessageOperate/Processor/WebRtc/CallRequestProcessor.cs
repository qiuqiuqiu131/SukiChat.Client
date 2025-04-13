using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class CallRequestProcessor(IContainerProvider container)
    : ProcessorBase<CallRequest>(container);