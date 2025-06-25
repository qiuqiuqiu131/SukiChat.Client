using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class AudioStateChangedProcessor(IContainerProvider container)
    : ProcessorBase<AudioStateChanged>(container);