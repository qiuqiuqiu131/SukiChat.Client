using Google.Protobuf;

namespace ChatClient.Tool.Events;

public class ResponseEvent<T> : PubSubEvent<T> where T : IMessage
{
    
}