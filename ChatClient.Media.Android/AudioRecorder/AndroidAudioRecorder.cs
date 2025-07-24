using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.Android.AudioRecorder;

public class AndroidAudioRecorder:IPlatformAudioRecorder
{
    public Stream? TargetStream { get; set; }
    public event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelDetected;
    public void StartRecording(int deviceNumber = 0, int sampleRate = 44100, int channels = 1)
    {
        throw new NotImplementedException();
    }

    public void StopRecording()
    {
        throw new NotImplementedException();
    }

    public void PauseRecording()
    {
        throw new NotImplementedException();
    }

    public void ResumeRecording()
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}