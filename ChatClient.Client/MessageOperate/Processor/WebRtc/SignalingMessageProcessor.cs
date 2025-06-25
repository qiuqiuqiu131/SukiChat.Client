using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class SignalingMessageProcessor(IContainerProvider container)
    : ProcessorBase<SignalingMessage>(container);