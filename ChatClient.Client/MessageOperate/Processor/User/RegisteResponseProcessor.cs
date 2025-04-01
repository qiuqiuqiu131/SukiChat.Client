using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class RegisteResponseProcessor(IContainerProvider container) 
    : ProcessorBase<RegisteResponse>(container);