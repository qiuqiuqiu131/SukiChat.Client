using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class HangUpProcessor(IContainerProvider container)
    : ProcessorBase<HangUp>(container);