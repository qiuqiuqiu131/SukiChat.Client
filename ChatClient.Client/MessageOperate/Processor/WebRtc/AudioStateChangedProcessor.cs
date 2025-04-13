using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class AudioStateChangedProcessor(IContainerProvider container)
    : ProcessorBase<AudioStateChanged>(container);