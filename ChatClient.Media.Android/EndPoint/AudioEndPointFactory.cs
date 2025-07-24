using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.EndPoint;

namespace ChatClient.Media.Android.EndPoint;

public class AudioEndPointFactory:IFactory<IAudioEndPoint>
{
    public IAudioEndPoint Create()
    {
        return new AndroidAudioEndPoint();
    }
}