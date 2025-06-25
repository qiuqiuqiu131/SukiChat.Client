using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class HangUpProcessor(IContainerProvider container)
    : ProcessorBase<HangUp>(container);