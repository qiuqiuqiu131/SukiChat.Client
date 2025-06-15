using NAudio.Lame;

namespace ChatClient.Tool.Media.Audio;

public interface IPlatformAudioRecorder : IDisposable
{
    Stream? TargetStream { get; set; }
    event EventHandler<RecordingStateChangedEventArgs> StateChanged;
    event EventHandler<AudioLevelEventArgs> AudioLevelDetected;

    void StartRecording(int deviceNumber = 0, int sampleRate = 44100, int channels = 1,
        LAMEPreset preset = LAMEPreset.STANDARD);

    void StopRecording();

    void PauseRecording();

    void ResumeRecording();
}