using Google.Protobuf;
using Prism.Events;

namespace ChatClient.Tool.Events;

public class ResponseEvent<T> : PubSubEvent<T> where T : IMessage
{
    
}