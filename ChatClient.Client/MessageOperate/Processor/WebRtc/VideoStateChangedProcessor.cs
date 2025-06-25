using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.WebRtc;

public class VideoStateChangedProcessor(IContainerProvider container)
    : ProcessorBase<VideoStateChanged>(container);