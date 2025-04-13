using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.WebRtc;

public class VideoStateChangedProcessor(IContainerProvider container)
    : ProcessorBase<VideoStateChanged>(container);