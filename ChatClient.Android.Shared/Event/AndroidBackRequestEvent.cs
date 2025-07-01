#if ANDROID
using Avalonia.Android;

namespace ChatClient.Android.Shared.Event;

public class AndroidBackRequestEvent:PubSubEvent<AndroidBackRequestedEventArgs>
{
    
}
#endif