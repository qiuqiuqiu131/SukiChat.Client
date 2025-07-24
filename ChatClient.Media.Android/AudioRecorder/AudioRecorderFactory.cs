using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.Android.AudioRecorder;

public class AudioRecorderFactory: IFactory<IPlatformAudioRecorder>
{
    public IPlatformAudioRecorder Create()
    {
        return new AndroidAudioRecorder();
    }
}