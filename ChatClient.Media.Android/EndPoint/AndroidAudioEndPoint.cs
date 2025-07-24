using System.Net;
using ChatClient.Tool.Media.EndPoint;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Android.EndPoint;

public class AndroidAudioEndPoint:IAudioEndPoint
{
    public Task PauseAudio()
    {
        throw new NotImplementedException();
    }

    public Task ResumeAudio()
    {
        throw new NotImplementedException();
    }

    public Task StartAudio()
    {
        throw new NotImplementedException();
    }

    public Task CloseAudio()
    {
        throw new NotImplementedException();
    }

    public List<AudioFormat> GetAudioSourceFormats()
    {
        throw new NotImplementedException();
    }

    public void SetAudioSourceFormat(AudioFormat audioFormat)
    {
        throw new NotImplementedException();
    }

    public List<AudioFormat> GetAudioSinkFormats()
    {
        throw new NotImplementedException();
    }

    public void SetAudioSinkFormat(AudioFormat audioFormat)
    {
        throw new NotImplementedException();
    }

    public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker,
        byte[] payload)
    {
        throw new NotImplementedException();
    }

    void IAudioSink.RestrictFormats(Func<AudioFormat, bool> filter)
    {
        throw new NotImplementedException();
    }

    public Task PauseAudioSink()
    {
        throw new NotImplementedException();
    }

    public Task ResumeAudioSink()
    {
        throw new NotImplementedException();
    }

    public Task StartAudioSink()
    {
        throw new NotImplementedException();
    }

    public Task CloseAudioSink()
    {
        throw new NotImplementedException();
    }

    public event SourceErrorDelegate? OnAudioSinkError;

    void IAudioSource.RestrictFormats(Func<AudioFormat, bool> filter)
    {
        throw new NotImplementedException();
    }

    public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
    {
        throw new NotImplementedException();
    }

    public bool HasEncodedAudioSubscribers()
    {
        throw new NotImplementedException();
    }

    public bool IsAudioSourcePaused()
    {
        throw new NotImplementedException();
    }

    public event EncodedSampleDelegate? OnAudioSourceEncodedSample;
    public event RawAudioSampleDelegate? OnAudioSourceRawSample;
    public event SourceErrorDelegate? OnAudioSourceError;
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public MediaEndPoints ToMediaEndPoints()
    {
        throw new NotImplementedException();
    }
}