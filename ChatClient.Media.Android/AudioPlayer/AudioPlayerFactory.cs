using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.Android.AudioPlayer;

public class AudioPlayerFactory: IFactory<IPlatformAudioPlayer>
{
    public IPlatformAudioPlayer Create()
    {
        // 在Android平台上，直接返回AndroidAudioPlayer实例
        return new AndroidAudioPlayer();
    }
}