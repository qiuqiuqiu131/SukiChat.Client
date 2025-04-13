using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class SignalingMessageProcessor(IContainerProvider container)
    : ProcessorBase<SignalingMessage>(container);