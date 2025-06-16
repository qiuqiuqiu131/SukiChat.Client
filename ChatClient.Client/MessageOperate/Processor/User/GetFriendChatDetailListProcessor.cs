using ChatClient.MessageOperate;
using ChatServer.Common.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient.MessageOperate.Processor.User
{
    class GetFriendChatDetailListProcessor(IContainerProvider container) : ProcessorBase<GetFriendChatDetailListResponse>(container)
    {
    }
}
