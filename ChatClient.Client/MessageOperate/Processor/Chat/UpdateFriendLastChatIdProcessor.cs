using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class UpdateFriendLastChatIdProcessor(IContainerProvider container)
    : ProcessorBase<UpdateFriendLastChatIdResponse>(container)
{
    protected override Task OnProcess(UpdateFriendLastChatIdResponse message)
    {
        return base.OnProcess(message);
    }
}