using ChatClient.Media.Android.AudioPlayer;
using ChatClient.Media.Android.AudioRecorder;
using ChatClient.Media.Android.EndPoint;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;
using ChatClient.Tool.Media.EndPoint;

namespace ChatClient.Media.Android;

public class MediaModule:IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IFactory<IPlatformAudioPlayer>, AudioPlayerFactory>()
            .Register<IFactory<IPlatformAudioRecorder>, AudioRecorderFactory>()
            .Register<IFactory<IAudioEndPoint>, AudioEndPointFactory>()
            .Register<IFactory<ICameraEndPoint>, CameraEndPointFactory>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}